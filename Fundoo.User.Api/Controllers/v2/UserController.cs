using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.User.Api.Controllers.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/user")]
    public class UserController : ControllerBase
    {
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new
            {
                apiVersion = "2.0",
                message = "Fundoo User Service API v2.0",
                features = new[] { "Enhanced Profile Metadata", "API Versioning", "Resilience Pipelines", "Rate Limiting", "SSO (Single Sign-On)", "2FA (TOTP & Email OTP)", "OAuth 2.0 M2M Authentication" },
                timestamp = DateTimeOffset.UtcNow
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public IActionResult ProfileV2()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;
            string? name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new
            {
                apiVersion = "2.0",
                user = new
                {
                    id = userId,
                    email = email,
                    fullName = name
                },
                metadata = new
                {
                    environment = "Development",
                    retrievedAt = DateTimeOffset.UtcNow
                }
            });
        }
    }
}
