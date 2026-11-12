using System;
using System.Linq;
using System.Text;
using Fundoo.Notification.Application.Feature.Notification.Services;
using Fundoo.Notification.Infrastructure.Email;
using Fundoo.Notification.Infrastructure.Email.Template;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Fundoo.Notification.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        builder.Services.Configure<SmtpSettings>(
            builder.Configuration.GetSection("SmtpSettings"));

        builder.Services.AddScoped<IEmailService, EmailService>();

        // Configure OAuth 2.0 JWT Bearer Authentication for Service-to-Service communication
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#";
        var validIssuers = new[]
        {
            builder.Configuration["Jwt:OAuthIssuer"] ?? "FundooOAuthServer",
            "FundooUserApi",
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

        // Granular OAuth Scope Authorization Policy for inter-service calls
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("NotificationSendScope", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var scopeClaim = context.User.FindFirst("scope")?.Value ?? "";
                    var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var scpClaims = context.User.FindAll("scp").Select(c => c.Value);
                    return scopes.Contains("notification:send", StringComparer.OrdinalIgnoreCase) ||
                           scpClaims.Contains("notification:send", StringComparer.OrdinalIgnoreCase);
                });
            });
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fundoo Notification Service API",
                Version = "v1",
                Description = "Notification Service protected via OAuth 2.0 Bearer tokens"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter OAuth 2.0 Bearer token (obtained from User OAuth token endpoint)"
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

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
