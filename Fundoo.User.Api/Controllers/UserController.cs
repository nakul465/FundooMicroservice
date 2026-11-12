using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Fundoo.User.Domain.Models;
using MediatR;
using Fundoo.User.Application.Command;
using Fundoo.User.Application.Services;

namespace Fundoo.User.Api.Controllers
{ 
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly INotesServiceClient? _notesClient;

        public UserController(ISender sender, INotesServiceClient? notesClient = null)
        {
            _sender = sender;
            _notesClient = notesClient;
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new
            {
                apiVersion = "1.0",
                message = "Fundoo User Service API v1.0",
                timestamp = DateTimeOffset.UtcNow
            });
        }

        [HttpPost]
        [Route("register")]
        [EnableRateLimiting("register-policy")]
        public async Task<ResponseModel<UserResponseModel>> RegisterUser(RegistrationModel registrationModel)
        {
            CreateUserCommand command = new CreateUserCommand(registrationModel);
            UserResponseModel response = await _sender.Send(command);

            ResponseModel<UserResponseModel> model = new ResponseModel<UserResponseModel>();
            model.Data = response;
            model.Success = true;
            model.Message = "User Registered Successfull";
            return model;
        }

        [HttpPost]
        [Route("login")]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            LoginCommand command = new LoginCommand(model);
            var result = await _sender.Send(command);
            var response = new ResponseModel<UserResponseModel>();

            if (result == null)
            {
                response.Success = false;
                response.Message = "Invalid email or password";
                return Unauthorized(response);
            }

            response.Success = true;
            response.Message = "Login successful";
            response.Data = result;
            return Ok(response);
        }   

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;
            string? name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new { userId, email, name });
        }

        [HttpPost("ForgotPassword")]
        [EnableRateLimiting("password-reset-policy")]
        public async Task<IActionResult> ForgotPasswordAsync(ForgotPasswordRequestModel req)
        {
            var res = await _sender.Send(new ForgotPasswordCommand(req));

            return Ok(res);
        }

        [HttpPost]
        [Route("ResetPassword")]
        [EnableRateLimiting("password-reset-policy")]
        public async Task<IActionResult> ResetPasswordAsync(ResetPasswordRequestModel req)
        {
            var res = await _sender.Send(new ResetPasswordCommand(req));
            if (res.Success)
            {
                return Ok(res);
            }
            return BadRequest(res);
        }

        // ==================== SSO (Single Sign-On) Endpoints ====================

        [HttpPost("sso/login")]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> SsoLogin([FromBody] SsoLoginRequestModel model)
        {
            var result = await _sender.Send(new SsoLoginCommand(model));
            return Ok(new ResponseModel<SsoLoginResponseModel>
            {
                Success = true,
                Message = result.IsNewUser ? "SSO User registered and logged in successfully" : "SSO Login successful",
                Data = result
            });
        }

        [Authorize]
        [HttpGet("sso/userinfo")]
        public IActionResult SsoUserInfo()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value ?? "";
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var name = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var provider = User.FindFirst("sso_provider")?.Value ?? "Fundoo";

            return Ok(new SsoUserInfoModel
            {
                Sub = sub,
                Email = email,
                Name = name,
                GivenName = name,
                FamilyName = "",
                EmailVerified = true,
                Provider = provider
            });
        }

        // ==================== 2FA (Two-Factor Authentication) Endpoints ====================

        [Authorize]
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseModel<object> { Success = false, Message = "Invalid user identity." });
            }

            var result = await _sender.Send(new TwoFactorSetupCommand(userId));
            return Ok(new ResponseModel<TwoFactorSetupResponseModel>
            {
                Success = true,
                Message = "TOTP secret generated successfully. Scan the QR code or enter manual key.",
                Data = result
            });
        }

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorEnableRequestModel model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseModel<object> { Success = false, Message = "Invalid user identity." });
            }

            var result = await _sender.Send(new TwoFactorEnableCommand(userId, model));
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableRequestModel model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ResponseModel<object> { Success = false, Message = "Invalid user identity." });
            }

            var result = await _sender.Send(new TwoFactorDisableCommand(userId, model));
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("2fa/verify-login")]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> VerifyTwoFactorLogin([FromBody] TwoFactorVerifyLoginRequestModel model)
        {
            var result = await _sender.Send(new TwoFactorVerifyLoginCommand(model));
            if (result == null)
            {
                return Unauthorized(new ResponseModel<object>
                {
                    Success = false,
                    Message = "Invalid 2FA code or expired 2FA session token."
                });
            }

            return Ok(new ResponseModel<UserResponseModel>
            {
                Success = true,
                Message = "2FA verification successful. Logged in.",
                Data = result
            });
        }

        [HttpPost("2fa/send-email-otp")]
        [EnableRateLimiting("password-reset-policy")]
        public async Task<IActionResult> SendTwoFactorEmailOtp([FromBody] TwoFactorSendEmailOtpRequestModel model)
        {
            var result = await _sender.Send(new TwoFactorSendEmailOtpCommand(model));
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        // ==================== OAuth Inter-Service Communication ====================

        [Authorize]
        [HttpGet("notes-summary")]
        public async Task<IActionResult> GetUserNotesSummary()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
            if (!int.TryParse(userIdStr, out var userId) || _notesClient == null)
            {
                return BadRequest(new ResponseModel<object> { Success = false, Message = "Notes client or user id unavailable." });
            }

            var count = await _notesClient.GetUserNotesCountAsync(userId);
            return Ok(new ResponseModel<object>
            {
                Success = true,
                Message = "User notes count retrieved via OAuth service-to-service call.",
                Data = new { userId, notesCount = count }
            });
        }
    }
}

