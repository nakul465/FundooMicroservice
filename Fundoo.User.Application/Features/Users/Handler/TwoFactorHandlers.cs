using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.User.Application.Command;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Handler
{
    public class TwoFactorSetupHandler : IRequestHandler<TwoFactorSetupCommand, TwoFactorSetupResponseModel>
    {
        private readonly IUserBL _userBL;
        private readonly ITwoFactorService _twoFactorService;

        public TwoFactorSetupHandler(IUserBL userBL, ITwoFactorService twoFactorService)
        {
            _userBL = userBL;
            _twoFactorService = twoFactorService;
        }

        public async Task<TwoFactorSetupResponseModel> Handle(TwoFactorSetupCommand command, CancellationToken cancellationToken)
        {
            var user = await _userBL.GetUserByIdBL(command.UserId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            var secret = _twoFactorService.GenerateSecretKey();
            user.TwoFactorSecret = secret;
            await _userBL.UpdateUserBL(user);

            var qrUri = _twoFactorService.GenerateQrCodeUri(user.Email, secret);

            return new TwoFactorSetupResponseModel
            {
                SecretKey = secret,
                ManualEntryKey = secret,
                QrCodeUri = qrUri,
                Email = user.Email
            };
        }
    }

    public class TwoFactorEnableHandler : IRequestHandler<TwoFactorEnableCommand, ResponseModel<bool>>
    {
        private readonly IUserBL _userBL;
        private readonly ITwoFactorService _twoFactorService;

        public TwoFactorEnableHandler(IUserBL userBL, ITwoFactorService twoFactorService)
        {
            _userBL = userBL;
            _twoFactorService = twoFactorService;
        }

        public async Task<ResponseModel<bool>> Handle(TwoFactorEnableCommand command, CancellationToken cancellationToken)
        {
            var user = await _userBL.GetUserByIdBL(command.UserId);
            if (user == null)
            {
                return new ResponseModel<bool> { Success = false, Message = "User not found." };
            }

            var type = (command.Model.TwoFactorType ?? "TOTP").ToUpperInvariant();
            bool isValid = false;

            if (type == "EMAIL")
            {
                isValid = _twoFactorService.ValidateEmailOtp(user.Email, command.Model.Code);
            }
            else
            {
                if (string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    return new ResponseModel<bool>
                    {
                        Success = false,
                        Message = "Two-factor secret has not been set up. Call /2fa/setup first."
                    };
                }
                isValid = _twoFactorService.ValidateTotpCode(user.TwoFactorSecret, command.Model.Code);
            }

            if (!isValid)
            {
                return new ResponseModel<bool>
                {
                    Success = false,
                    Message = "Invalid verification code. Please try again."
                };
            }

            user.IsTwoFactorEnabled = true;
            user.TwoFactorType = type == "EMAIL" ? "Email" : "TOTP";
            await _userBL.UpdateUserBL(user);

            return new ResponseModel<bool>
            {
                Success = true,
                Message = $"Two-factor authentication ({user.TwoFactorType}) enabled successfully.",
                Data = true
            };
        }
    }

    public class TwoFactorDisableHandler : IRequestHandler<TwoFactorDisableCommand, ResponseModel<bool>>
    {
        private readonly IUserBL _userBL;
        private readonly ITwoFactorService _twoFactorService;

        public TwoFactorDisableHandler(IUserBL userBL, ITwoFactorService twoFactorService)
        {
            _userBL = userBL;
            _twoFactorService = twoFactorService;
        }

        public async Task<ResponseModel<bool>> Handle(TwoFactorDisableCommand command, CancellationToken cancellationToken)
        {
            var user = await _userBL.GetUserByIdBL(command.UserId);
            if (user == null)
            {
                return new ResponseModel<bool> { Success = false, Message = "User not found." };
            }

            bool isVerified = false;
            if (!string.IsNullOrWhiteSpace(command.Model.Password))
            {
                isVerified = BCrypt.Net.BCrypt.Verify(command.Model.Password, user.Password);
            }
            else if (!string.IsNullOrWhiteSpace(command.Model.Code))
            {
                if (user.TwoFactorType == "Email")
                {
                    isVerified = _twoFactorService.ValidateEmailOtp(user.Email, command.Model.Code);
                }
                else if (!string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    isVerified = _twoFactorService.ValidateTotpCode(user.TwoFactorSecret, command.Model.Code);
                }
            }

            if (!isVerified)
            {
                return new ResponseModel<bool>
                {
                    Success = false,
                    Message = "Invalid password or verification code provided to disable 2FA."
                };
            }

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorType = null;
            await _userBL.UpdateUserBL(user);

            return new ResponseModel<bool>
            {
                Success = true,
                Message = "Two-factor authentication disabled successfully.",
                Data = true
            };
        }
    }

    public class TwoFactorVerifyLoginHandler : IRequestHandler<TwoFactorVerifyLoginCommand, UserResponseModel?>
    {
        private readonly IUserBL _userBL;
        private readonly IJwtService _jwtService;
        private readonly ITwoFactorService _twoFactorService;

        public TwoFactorVerifyLoginHandler(
            IUserBL userBL,
            IJwtService jwtService,
            ITwoFactorService twoFactorService)
        {
            _userBL = userBL;
            _jwtService = jwtService;
            _twoFactorService = twoFactorService;
        }

        public async Task<UserResponseModel?> Handle(TwoFactorVerifyLoginCommand command, CancellationToken cancellationToken)
        {
            var principal = _jwtService.ValidateTwoFactorSessionToken(command.Model.TwoFactorSessionToken);
            if (principal == null)
            {
                return null;
            }

            var userIdStr = principal.FindFirst("user_id")?.Value
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdStr, out var userId))
            {
                return null;
            }

            var user = await _userBL.GetUserByIdBL(userId);
            if (user == null) return null;

            var twoFactorType = user.TwoFactorType ?? "TOTP";
            bool isValid = false;

            if (twoFactorType == "Email")
            {
                isValid = _twoFactorService.ValidateEmailOtp(user.Email, command.Model.Code);
            }
            else
            {
                if (string.IsNullOrEmpty(user.TwoFactorSecret)) return null;
                isValid = _twoFactorService.ValidateTotpCode(user.TwoFactorSecret, command.Model.Code);
            }

            if (!isValid) return null;

            var response = new UserResponseModel
            {
                UserId = user.UserId,
                Email = user.Email,
                Fname = user.FirstName,
                Lname = user.LastName,
                ContactNo = user.PhoneNumber,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                SsoProvider = user.SsoProvider
            };

            response.Token = _jwtService.GenerateToken(response);
            return response;
        }
    }

    public class TwoFactorSendEmailOtpHandler : IRequestHandler<TwoFactorSendEmailOtpCommand, ResponseModel<bool>>
    {
        private readonly IUserBL _userBL;
        private readonly IJwtService _jwtService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IEmailService _emailService;

        public TwoFactorSendEmailOtpHandler(
            IUserBL userBL,
            IJwtService jwtService,
            ITwoFactorService twoFactorService,
            IEmailService emailService)
        {
            _userBL = userBL;
            _jwtService = jwtService;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
        }

        public async Task<ResponseModel<bool>> Handle(TwoFactorSendEmailOtpCommand command, CancellationToken cancellationToken)
        {
            var principal = _jwtService.ValidateTwoFactorSessionToken(command.Model.TwoFactorSessionToken);
            if (principal == null)
            {
                return new ResponseModel<bool> { Success = false, Message = "Invalid or expired 2FA session token." };
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return new ResponseModel<bool> { Success = false, Message = "Session token does not contain a valid email." };
            }

            var otp = _twoFactorService.GenerateEmailOtp(email);
            var emailRes = await _emailService.SendEmailAsync(new EmailRequestModel
            {
                To = email,
                Subject = "Fundoo Two-Factor Authentication Code",
                Body = $"Your login verification code is: {otp}. This code expires in 5 minutes.",
                IsHtml = false
            });

            return new ResponseModel<bool>
            {
                Success = emailRes.Success,
                Message = emailRes.Success ? "Verification code sent to your email." : "Failed to send verification code email.",
                Data = emailRes.Success
            };
        }
    }
}
