using System.Threading;
using System.Threading.Tasks;
using Fundoo.User.Application.Command;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using MediatR;

namespace Fundoo.User.Application.Handler
{
    public class LoginHandler : IRequestHandler<LoginCommand, UserResponseModel?>
    {
        private readonly IJwtService _jwtService;
        private readonly IUserBL _userBL;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IEmailService _emailService;

        public LoginHandler(
            IJwtService jwtService,
            IUserBL userBL,
            ITwoFactorService twoFactorService,
            IEmailService emailService)
        {
            _jwtService = jwtService;
            _userBL = userBL;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
        }

        public async Task<UserResponseModel?> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            var user = await _userBL.LoginUserBL(command._loginModel);
            if (user == null) return null;

            bool isValid = BCrypt.Net.BCrypt.Verify(command._loginModel.Password, user.Password);
            if (!isValid) return null;

            var response = new UserResponseModel
            {
                UserId = user.UserId,
                ContactNo = user.PhoneNumber,
                Email = user.Email,
                Fname = user.FirstName,
                Lname = user.LastName,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                SsoProvider = user.SsoProvider
            };

            // If 2FA is enabled, return temporary session token instead of final JWT
            if (user.IsTwoFactorEnabled)
            {
                response.RequiresTwoFactor = true;
                response.TwoFactorType = user.TwoFactorType ?? "TOTP";
                response.TwoFactorSessionToken = _jwtService.GenerateTwoFactorSessionToken(
                    user.UserId,
                    user.Email,
                    response.TwoFactorType);

                if (response.TwoFactorType == "Email")
                {
                    try
                    {
                        var otp = _twoFactorService.GenerateEmailOtp(user.Email);
                        await _emailService.SendEmailAsync(new EmailRequestModel
                        {
                            To = user.Email,
                            Subject = "Fundoo Two-Factor Authentication Code",
                            Body = $"Your login verification code is: {otp}. This code expires in 5 minutes.",
                            IsHtml = false
                        });
                    }
                    catch
                    {
                        // Log or proceed so user can resend if needed
                    }
                }

                return response;
            }

            response.Token = _jwtService.GenerateToken(response);
            return response;
        }
    }
}
