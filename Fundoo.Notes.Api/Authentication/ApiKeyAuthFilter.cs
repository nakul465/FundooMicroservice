using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Fundoo.Notes.Api.Authentication
{
    public class ApiKeyAuthFilter : IAsyncActionFilter
    {
        public const string SecretKeyHeaderName = "X-Internal-Secret-Key";
        public const string UserIdHeaderName = "X-User-Id";
        private readonly IConfiguration _configuration;

        public ApiKeyAuthFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var hasSecret = request.Headers.TryGetValue(SecretKeyHeaderName, out var extractedKey);
            var hasUserId = request.Headers.ContainsKey(UserIdHeaderName);
            var authHeader = request.Headers.TryGetValue("Authorization", out var authValues) ? authValues.ToString() : "";
            var tokenHeader = request.Headers.TryGetValue("token", out var tokenValues) ? tokenValues.ToString() : "";

            // If user is already authenticated via JWT Bearer or a token is provided in headers, defer to JWT
            if (context.HttpContext.User.Identity?.IsAuthenticated == true ||
                (!string.IsNullOrEmpty(authHeader) && (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || authHeader.Length > 20)) ||
                !string.IsNullOrEmpty(tokenHeader))
            {
                if (request.Headers.TryGetValue(UserIdHeaderName, out var extractedUserIdFromAuth) &&
                    int.TryParse(extractedUserIdFromAuth, out var uId) && uId > 0)
                {
                    context.HttpContext.Items["UserId"] = uId;
                }
                await next();
                return;
            }
            if (!hasSecret)
            {
                // #region agent log
                try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", hypothesisId = "A", location = "ApiKeyAuthFilter.cs:missing-secret", message = "Rejecting because X-Internal-Secret-Key is missing", data = new { path = request.Path.Value }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unauthorized: Missing Authorization Bearer token or X-Internal-Secret-Key header."
                });
                return;
            }

            var configuredKey = _configuration["InternalServiceAuth:SecretKey"] ?? "FundooSecretKeyForJwtAuthentication2026!#";
            if (string.IsNullOrEmpty(configuredKey) || !configuredKey.Equals(extractedKey.ToString()))
            {
                // #region agent log
                try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", hypothesisId = "A", location = "ApiKeyAuthFilter.cs:invalid-secret", message = "Rejecting because internal secret key does not match", data = new { configuredKeyPresent = !string.IsNullOrEmpty(configuredKey), extractedKeyLength = extractedKey.ToString().Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unauthorized: Invalid internal secret key."
                });
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(UserIdHeaderName, out var extractedUserId) ||
                !int.TryParse(extractedUserId, out var userId) || userId <= 0)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Bad Request: A valid positive integer X-User-Id header is required."
                });
                return;
            }

            context.HttpContext.Items["UserId"] = userId;
            // #region agent log
            try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", runId = "post-fix", hypothesisId = "A", location = "ApiKeyAuthFilter.cs:authorized", message = "Notes auth filter accepted request", data = new { path = request.Path.Value, userId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion

            await next();
        }
    }
}
