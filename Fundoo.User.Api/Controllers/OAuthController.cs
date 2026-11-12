using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.User.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/oauth")]
    [Route("api/v{version:apiVersion}/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuthClientStore _clientStore;
        private readonly IJwtService _jwtService;

        public OAuthController(IOAuthClientStore clientStore, IJwtService jwtService)
        {
            _clientStore = clientStore;
            _jwtService = jwtService;
        }

        [HttpPost("token")]
        [Consumes("application/x-www-form-urlencoded", "application/json")]
        public async Task<IActionResult> Token([FromForm] OAuthTokenRequestModel? formModel, [FromBody] OAuthTokenRequestModel? jsonModel)
        {
            var model = formModel ?? jsonModel ?? new OAuthTokenRequestModel();

            // Support HTTP Basic Authentication for client credentials (RFC 6749 Section 2.3.1)
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var base64Credentials = authHeader.Substring("Basic ".Length).Trim();
                    var credentialBytes = Convert.FromBase64String(base64Credentials);
                    var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                    if (credentials.Length == 2)
                    {
                        model.ClientId = credentials[0];
                        model.ClientSecret = credentials[1];
                    }
                }
                catch
                {
                    return BadRequest(new { error = "invalid_client", error_description = "Malformed Basic authorization header." });
                }
            }

            if (!string.Equals(model.GrantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "unsupported_grant_type", error_description = "Only 'client_credentials' grant_type is supported for machine-to-machine calls." });
            }

            if (string.IsNullOrWhiteSpace(model.ClientId) || string.IsNullOrWhiteSpace(model.ClientSecret))
            {
                return BadRequest(new { error = "invalid_client", error_description = "Missing client_id or client_secret." });
            }

            var isValid = await _clientStore.ValidateCredentialsAsync(model.ClientId, model.ClientSecret);
            if (!isValid)
            {
                return Unauthorized(new { error = "invalid_client", error_description = "Invalid client credentials." });
            }

            var client = await _clientStore.GetClientAsync(model.ClientId);
            if (client == null)
            {
                return Unauthorized(new { error = "invalid_client", error_description = "Client record not found." });
            }

            // Determine scopes
            var requestedScopes = string.IsNullOrWhiteSpace(model.Scope)
                ? client.AllowedScopes
                : model.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            var grantedScopes = requestedScopes.Where(s => client.AllowedScopes.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
            if (!grantedScopes.Any())
            {
                return BadRequest(new { error = "invalid_scope", error_description = "None of the requested scopes are allowed for this client." });
            }

            const int expiresIn = 1800; // 30 minutes
            var accessToken = _jwtService.GenerateOAuthToken(client.ClientId, grantedScopes, client.AllowedAudiences, expiresIn);

            return Ok(new OAuthTokenResponseModel
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                Scope = string.Join(" ", grantedScopes)
            });
        }
    }
}
