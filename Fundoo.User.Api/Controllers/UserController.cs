using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
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
        private readonly IConfiguration? _configuration;

        public UserController(ISender sender, INotesServiceClient? notesClient = null, IConfiguration? configuration = null)
        {
            _sender = sender;
            _notesClient = notesClient;
            _configuration = configuration;
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

        // ==================== Real Google OAuth 2.0 / SSO Endpoints ====================

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

        /// <summary>
        /// Initiates the OAuth 2.0 redirect flow with Google.
        /// When hit in a browser, redirects to accounts.google.com for login.
        /// </summary>
        [HttpGet("login-google")]
        public IActionResult LoginGoogle([FromQuery] string? returnUrl = null)
        {
            var googleClientId = _configuration?["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId) || googleClientId.StartsWith("YOUR_GOOGLE_CLIENT_ID", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ResponseModel<object>
                {
                    Success = false,
                    Message = "Google OAuth is not configured. Please specify your Google ClientId and ClientSecret under Authentication:Google in appsettings.json.",
                    Data = null
                });
            }

            var redirectUrl = Url.Action(nameof(GoogleCallback), "User", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Callback endpoint that Google redirects back to after user authenticates on Google.
        /// </summary>
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null)
        {
            var authResult = await HttpContext.AuthenticateAsync("ExternalCookies");
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                return Unauthorized(new ResponseModel<object>
                {
                    Success = false,
                    Message = "Google OAuth authentication failed or was cancelled.",
                    Data = null
                });
            }

            var claims = authResult.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Google User";
            var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ResponseModel<object>
                {
                    Success = false,
                    Message = "Email claim not received from Google account.",
                    Data = null
                });
            }

            var names = name.Split(' ', 2);
            var firstName = names.Length > 0 ? names[0] : name;
            var lastName = names.Length > 1 ? names[1] : "";

            var result = await _sender.Send(new SsoLoginCommand(new SsoLoginRequestModel
            {
                Provider = "Google",
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IdToken = googleId
            }));

            // Sign out of the temporary external cookie
            await HttpContext.SignOutAsync("ExternalCookies");

            return Ok(new ResponseModel<SsoLoginResponseModel>
            {
                Success = true,
                Message = result.RequiresTwoFactor
                    ? "Google authentication verified. Two-Factor Authentication required."
                    : "Logged in successfully via Google OAuth.",
                Data = result
            });
        }

        /// <summary>
        /// Direct Google Token Verification for Postman, Mobile apps, or SPAs.
        /// Accepts a Google OAuth access_token or id_token, verifies it against Google API, and logs the user in.
        /// </summary>
        [HttpPost("sso/google-token")]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> GoogleTokenLogin([FromBody] GoogleTokenLoginRequestModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Token))
            {
                return BadRequest(new ResponseModel<object>
                {
                    Success = false,
                    Message = "Google OAuth token is required.",
                    Data = null
                });
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", model.Token);
                var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback check as id_token endpoint
                    var idTokenRes = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(model.Token)}");
                    if (!idTokenRes.IsSuccessStatusCode)
                    {
                        return Unauthorized(new ResponseModel<object>
                        {
                            Success = false,
                            Message = "Invalid or expired Google OAuth token.",
                            Data = null
                        });
                    }
                    response = idTokenRes;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;

                var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
                var name = root.TryGetProperty("name", out var n) ? n.GetString() : "Google User";
                var sub = root.TryGetProperty("sub", out var s) ? s.GetString() : Guid.NewGuid().ToString();

                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "Could not extract email from Google token.",
                        Data = null
                    });
                }

                var names = name?.Split(' ', 2) ?? new[] { "Google", "User" };
                var firstName = names.Length > 0 ? names[0] : "Google";
                var lastName = names.Length > 1 ? names[1] : "";

                var result = await _sender.Send(new SsoLoginCommand(new SsoLoginRequestModel
                {
                    Provider = "Google",
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    IdToken = sub
                }));

                return Ok(new ResponseModel<SsoLoginResponseModel>
                {
                    Success = true,
                    Message = result.RequiresTwoFactor
                        ? "Google OAuth token verified. Two-Factor Authentication required."
                        : "Logged in successfully with Google OAuth.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel<object>
                {
                    Success = false,
                    Message = $"Error verifying Google token: {ex.Message}",
                    Data = null
                });
            }
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

