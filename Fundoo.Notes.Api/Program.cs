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
        var validIssuers = new[]
        {
            builder.Configuration["Jwt:OAuthIssuer"] ?? "FundooOAuthServer",
            builder.Configuration["Jwt:Issuer"] ?? "FundooUserApi",
            "FundooApp"
        };

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuers = validIssuers,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(2)
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

        app.UseHttpsRedirection();

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
