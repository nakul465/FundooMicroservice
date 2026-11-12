using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Fundoo.User.Application.Handler;
using Fundoo.User.Application.Services;
using Fundoo.User.Infrastructure.Email;
using Fundoo.User.Infrastructure.Persistence;
using Fundoo.User.Infrastructure.Persistence.Repositories;
using Fundoo.User.Infrastructure.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;

namespace Fundoo.User.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // Configure API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version")
            );
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // HttpClient for Notification Service with Resilience Pipeline
        builder.Services.AddHttpClient<IEmailService, EmailService>((services, client) =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            var baseUrl = configuration["NotificationService:BaseUrl"];

            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<OAuthDelegatingHandler>()
        .AddResilienceHandler("NotificationServicePipeline", pipelineBuilder =>
        {
            // 1. Outbound Rate Limiting (Token Bucket: max 20 requests per minute with burst up to 20)
            pipelineBuilder.AddRateLimiter(new TokenBucketRateLimiter(
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 20,
                    QueueLimit = 0,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    TokensPerPeriod = 20,
                    AutoReplenishment = true
                }));

            // 2. Retry with Exponential Backoff + Jitter for transient errors (5xx, 408, network drops)
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2)
            });

            // 3. Circuit Breaker (Trips open if >= 50% requests fail over a 10s window with at least 4 requests)
            pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(15)
            });

            // 4. Per-Attempt Timeout (Limits each individual try to 5 seconds)
            pipelineBuilder.AddTimeout(new HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(5)
            });
        });

        // HttpClient for Notes Service via OAuth M2M
        builder.Services.AddHttpClient<INotesServiceClient, NotesServiceClient>((services, client) =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["NotesService:BaseUrl"] ?? "http://localhost:5025";
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<OAuthDelegatingHandler>();

        // Configure Rate Limiting for Auth Endpoints (Login, Register, Forgot/Reset Password)
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Too many requests. Please try again later."
                }, cancellationToken: token);
            };

            // Login policy: max 5 requests per 1 minute per IP (prevents brute-force)
            options.AddPolicy("login-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Register policy: max 5 requests per hour per IP (prevents bot account creation)
            options.AddPolicy("register-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    }));

            // Password reset / Forgot password policy: max 3 requests per 15 minutes per IP (prevents email bombing)
            options.AddPolicy("password-reset-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));
        });

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly));

        builder.Services.AddScoped<IUserBL, UserBL>();
        builder.Services.AddScoped<IUserRL, UserRL>();
        builder.Services.AddSingleton<IJwtService, JwtService>();
        builder.Services.AddSingleton<ITwoFactorService, TwoFactorService>();
        builder.Services.AddSingleton<IOAuthClientStore, OAuthClientStore>();
        builder.Services.AddSingleton<IOAuthTokenService, OAuthTokenService>();
        builder.Services.AddTransient<OAuthDelegatingHandler>();

        builder.Services.AddDbContext<FundooContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("FundooConnection"))
        );

        builder.Services.AddEndpointsApiExplorer();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuers = new[]
                    {
                        builder.Configuration["Jwt:Issuer"] ?? "FundooUserApi",
                        builder.Configuration["Jwt:OAuthIssuer"] ?? "FundooOAuthServer",
                        "FundooApp"
                    },
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#")),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fundoo User Service API - v1",
                Version = "v1",
                Description = "User Authentication and Account Management (Version 1.0)"
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Fundoo User Service API - v2",
                Version = "v2",
                Description = "User Authentication and Account Management (Version 2.0)"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT Bearer token only (no need to type Bearer)"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fundoo User API v1");
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "Fundoo User API v2");
            });
        }

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
