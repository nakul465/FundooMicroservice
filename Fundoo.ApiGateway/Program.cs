using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot Configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Preserve original JWT claim names (like user_id)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configure JWT Authentication for Ocelot Gateway
var jwtKey = builder.Configuration["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FundooApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FundooUsers";
// #region agent log
try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", runId = "post-fix", hypothesisId = "B", location = "ApiGateway/Program.cs:startup", message = "Gateway JWT validation config", data = new { issuer = jwtIssuer, audience = jwtAudience, keyLength = jwtKey.Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
// #endregion

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = false, // Disabled to prevent timezone/clock differences from rejecting tokens
            ValidateIssuerSigningKey = true,
            ValidIssuers = new[] { jwtIssuer, "FundooUserApi", "FundooOAuthServer" },
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromHours(24)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var auth = context.Request.Headers["Authorization"].ToString();
                // #region agent log
                try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", hypothesisId = "B", location = "ApiGateway/Program.cs:OnMessageReceived", message = "Gateway received Authorization header", data = new { path = context.Request.Path.Value, hasAuthorization = !string.IsNullOrEmpty(auth), startsWithBearer = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase), authorizationLength = auth.Length, tokenLength = string.IsNullOrEmpty(context.Token) ? 0 : context.Token.Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[ApiGateway JWT Error]: {context.Exception.Message}");
                // #region agent log
                try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", hypothesisId = "B", location = "ApiGateway/Program.cs:OnAuthenticationFailed", message = "Gateway JWT validation failed", data = new { path = context.Request.Path.Value, exceptionType = context.Exception.GetType().Name, exceptionMessage = context.Exception.Message }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("user_id")?.Value;
                var nameId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                // #region agent log
                try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", runId = "post-fix", hypothesisId = "E", location = "ApiGateway/Program.cs:OnTokenValidated", message = "Gateway JWT validated", data = new { path = context.Request.Path.Value, hasUserIdClaim = !string.IsNullOrEmpty(userId), userIdClaim = userId, nameIdentifier = nameId, claimTypes = context.Principal?.Claims.Select(c => c.Type).ToArray() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"[ApiGateway JWT Challenge]: Error='{context.Error}', Description='{context.ErrorDescription}'");
                // #region agent log
                try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", hypothesisId = "B", location = "ApiGateway/Program.cs:OnChallenge", message = "Gateway JWT challenge (unauthorized)", data = new { path = context.Request.Path.Value, error = context.Error, errorDescription = context.ErrorDescription }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return Task.CompletedTask;
            }
        };
    });

// Register Ocelot Services
builder.Services.AddOcelot(builder.Configuration);

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/notes"))
    {
        var authResult = await context.AuthenticateAsync("Bearer");
        if (authResult.Succeeded && authResult.Principal is not null)
        {
            context.User = authResult.Principal;
            var notesSecret = builder.Configuration["InternalServiceAuth:SecretKey"]
                              ?? "FundooSecretKeyForJwtAuthentication2026!#";
            context.Request.Headers["X-Internal-Secret-Key"] = notesSecret;
            var forwardedUserId = authResult.Principal.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(forwardedUserId))
            {
                context.Request.Headers["X-User-Id"] = forwardedUserId;
            }
            // #region agent log
            try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", runId = "post-fix", hypothesisId = "F", location = "ApiGateway/Program.cs:forward-notes-headers", message = "Gateway attached Notes internal headers", data = new { path = context.Request.Path.Value, hasSecretHeader = context.Request.Headers.ContainsKey("X-Internal-Secret-Key"), hasUserIdHeader = context.Request.Headers.ContainsKey("X-User-Id"), forwardedUserId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }
    }

    await next();
});

await app.UseOcelot();

app.Run();
