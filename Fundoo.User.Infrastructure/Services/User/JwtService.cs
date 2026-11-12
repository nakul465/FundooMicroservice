using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration user)
        {
            _config = user;
        }

        public string GenerateToken(UserResponseModel user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim("user_id", user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Fname),
                new Claim("is_two_factor_enabled", user.IsTwoFactorEnabled.ToString().ToLower())
            };
            if (!string.IsNullOrEmpty(user.SsoProvider))
            {
                claims.Add(new Claim("sso_provider", user.SsoProvider));
            }
            var issuer = _config["Jwt:Issuer"] ?? "FundooUserApi";
            var audience = _config["Jwt:Audience"] ?? "FundooUserClient";
            var jwtKey = _config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#";
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
                signingCredentials: credentials
            );

            var written = new JwtSecurityTokenHandler().WriteToken(token);
            // #region agent log
            try { System.IO.File.AppendAllText("/Users/nakularora/Documents/FundooMicroService/.cursor/debug-b18336.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "b18336", hypothesisId = "B", location = "JwtService.cs:GenerateToken", message = "Login JWT generated", data = new { issuer, audience, keyLength = jwtKey.Length, userId = user.UserId, tokenLength = written.Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            return written;
        }

        public string GeneratePasswordResetToken(string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("purpose", "password_reset"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "FundooUserApi",
                audience: _config["Jwt:Audience"] ?? "FundooUserClient",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string? ValidatePasswordResetToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));

                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"] ?? "FundooUserApi",
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"] ?? "FundooUserClient",
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(
                    token, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
                {
                    return null;
                }

                var purpose = principal.FindFirst("purpose")?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;

                if (purpose != "password_reset" ||
                    string.IsNullOrWhiteSpace(email))
                {
                    return null;
                }

                return email;
            }
            catch (Exception ex) when (
                ex is SecurityTokenException ||
                ex is ArgumentException ||
                ex is FormatException)
            {
                return null;
            }
        }

        public string GenerateTwoFactorSessionToken(int userId, string email, string twoFactorType)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("user_id", userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("two_factor_type", twoFactorType),
                new Claim("purpose", "two_factor_session"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "FundooUserApi",
                audience: _config["Jwt:Audience"] ?? "FundooUserClient",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5), // 5 minutes valid session
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateTwoFactorSessionToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));

                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"] ?? "FundooUserApi",
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"] ?? "FundooUserClient",
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
                {
                    return null;
                }

                var purpose = principal.FindFirst("purpose")?.Value;
                if (purpose != "two_factor_session")
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateOAuthToken(string clientId, IEnumerable<string> scopes, IEnumerable<string> audiences, int expirySeconds = 1800)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var scopeList = scopes?.ToList() ?? new List<string>();
            var audienceList = audiences?.ToList() ?? new List<string> { "FundooResources" };

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim("client_id", clientId),
                new Claim("scope", string.Join(" ", scopeList)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var scope in scopeList)
            {
                claims.Add(new Claim("scp", scope));
            }

            foreach (var aud in audienceList)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
            }

            var issuer = _config["Jwt:OAuthIssuer"] ?? "FundooOAuthServer";

            var token = new JwtSecurityToken(
                issuer: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(expirySeconds),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "FundooSecretKeyForJwtAuthentication2026!#"));

                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false, // Allow multi-issuer
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}
