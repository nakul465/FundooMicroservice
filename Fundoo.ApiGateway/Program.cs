using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot Configuration based on Environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("ocelot.Development.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true);
}
else
{
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
}
builder.Configuration.AddEnvironmentVariables();

// Preserve original JWT claim names (like user_id)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configure JWT Authentication for Ocelot Gateway
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key configuration is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("AllowAll");

// Unified Swagger UI on API Gateway
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/user/v1/swagger.json", "Fundoo User Service API - v1");
    c.SwaggerEndpoint("/swagger/user/v2/swagger.json", "Fundoo User Service API - v2");
    c.SwaggerEndpoint("/swagger/notes/v1/swagger.json", "Fundoo Notes Service API");
    c.SwaggerEndpoint("/swagger/notification/v1/swagger.json", "Fundoo Notification Service API");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Fundoo API Gateway - Unified Swagger";
});

// Redirect root to Swagger UI
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" || context.Request.Path == "")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/notes"))
    {
        var authResult = await context.AuthenticateAsync("Bearer");
        if (authResult.Succeeded && authResult.Principal is not null)
        {
            context.User = authResult.Principal;
            var notesSecret = builder.Configuration["InternalServiceAuth:SecretKey"];
            if (!string.IsNullOrEmpty(notesSecret))
            {
                context.Request.Headers["X-Internal-Secret-Key"] = notesSecret;
            }
            var forwardedUserId = authResult.Principal.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(forwardedUserId))
            {
                context.Request.Headers["X-User-Id"] = forwardedUserId;
            }
        }
        else
        {
            context.Request.Headers.Remove("X-Internal-Secret-Key");
            context.Request.Headers.Remove("X-User-Id");
        }
    }

    await next();
});

await app.UseOcelot();

app.Run();
