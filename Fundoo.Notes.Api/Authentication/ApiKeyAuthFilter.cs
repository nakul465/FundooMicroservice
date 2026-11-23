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
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unauthorized: Missing Authorization Bearer token or X-Internal-Secret-Key header."
                });
                return;
            }

            var configuredKey = _configuration["InternalServiceAuth:SecretKey"];
            if (string.IsNullOrEmpty(configuredKey) || !configuredKey.Equals(extractedKey.ToString()))
            {
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
            await next();
        }
    }
}
