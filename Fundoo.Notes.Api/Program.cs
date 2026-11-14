using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fundoo.Notes.Api.Authentication;
using Fundoo.Notes.Application.Features.Notes.Handler;
using Fundoo.Notes.Application.Features.Notes.Services;
using Fundoo.Notes.Infrastructure.Persistence.Repository;
using Fundoo.Notes.Infrastructure.Services.Notes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Fundoo.Notes.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Preserve original inbound claim names
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        // Add services to the container.
        builder.Services.AddControllers();

        // Read PostgreSQL connection string
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? builder.Configuration.GetConnectionString("FundooNotesConnection")
            ?? throw new InvalidOperationException("DefaultConnection / FundooNotesConnection connection string is missing.");

        // Register Health Checks
        builder.Services.AddHealthChecks()
            // Liveness check
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: new[] { "live" }
            )
            // PostgreSQL readiness check
            .AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: new[] { "ready" },
                timeout: TimeSpan.FromSeconds(3)
            );

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateNoteHandler).Assembly));

        builder.Services.AddScoped<INotesRL, NotesRL>();
        builder.Services.AddScoped<INotesBL, NotesBL>();
        builder.Services.AddScoped<ApiKeyAuthFilter>();

        // Configure OAuth 2.0 JWT Bearer Authentication
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#";
        var signingKeys = new List<SecurityKey>
        {
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("FundooSecretKeyForJwtAuthentication2026!#")),
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("FundooSuperSecretKeyJwtTokenAuth"))
        };
        if (!string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        {
            var confKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
            if (!signingKeys.Any(k => ((SymmetricSecurityKey)k).Key.SequenceEqual(confKey)))
            {
                signingKeys.Add(new SymmetricSecurityKey(confKey));
            }
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    ClockSkew = TimeSpan.FromHours(24)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var auth = context.Request.Headers["Authorization"].ToString();
                            if (!string.IsNullOrEmpty(auth))
                            {
                                context.Token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                                    ? auth.Substring("Bearer ".Length).Trim()
                                    : auth.Trim();
                            }
                            else if (context.Request.Headers.TryGetValue("token", out var tokenVal))
                            {
                                context.Token = tokenVal.ToString().Trim();
                            }
                        }
                        try
                        {
                            File.AppendAllText("/tmp/fundoo_notes_auth.log", $"[{DateTime.UtcNow:O}] OnMessageReceived: Path={context.Request.Path}, HasToken={!string.IsNullOrEmpty(context.Token)}, TokenLen={context.Token?.Length}\n");
                        }
                        catch { }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        try
                        {
                            File.AppendAllText("/tmp/fundoo_notes_auth.log", $"[{DateTime.UtcNow:O}] OnAuthenticationFailed: {context.Exception.GetType().Name} - {context.Exception.Message}\nInner: {context.Exception.InnerException?.Message}\n");
                        }
                        catch { }
                        Console.WriteLine($"[Notes.Api JWT Auth Failed]: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        try
                        {
                            File.AppendAllText("/tmp/fundoo_notes_auth.log", $"[{DateTime.UtcNow:O}] OnChallenge: Error='{context.Error}', Desc='{context.ErrorDescription}', AuthFailure={context.AuthenticateFailure?.Message}\n");
                        }
                        catch { }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var uId = context.Principal?.FindFirst("user_id")?.Value
                               ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? context.Principal?.FindFirst("sub")?.Value;
                        try
                        {
                            File.AppendAllText("/tmp/fundoo_notes_auth.log", $"[{DateTime.UtcNow:O}] OnTokenValidated: UserId={uId}\n");
                        }
                        catch { }
                        Console.WriteLine($"[Notes.Api JWT Validated for userId: {uId}]");
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fundoo Notes Service API",
                Version = "v1",
                Description = "Notes Management Service protected via OAuth 2.0 Bearer tokens"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter OAuth 2.0 Bearer token (JWT)"
            });

            options.AddSecurityDefinition("InternalSecretKey", new OpenApiSecurityScheme
            {
                Name = "X-Internal-Secret-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Legacy Internal Secret Key for backwards-compatible inter-service communication"
            });

            options.AddSecurityDefinition("UserIdHeader", new OpenApiSecurityScheme
            {
                Name = "X-User-Id",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "User Id forwarded from Gateway/User Service"
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
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "InternalSecretKey"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "UserIdHeader"
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
            app.UseSwaggerUI();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Liveness endpoint
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        // Readiness endpoint
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.Run();
    }
}
