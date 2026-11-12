using System.Security.Claims;
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Application.Services
{
	public interface IJwtService
	{
        string GenerateToken(UserResponseModel user);

        string GeneratePasswordResetToken(string email);

        string? ValidatePasswordResetToken(string token);

        string GenerateTwoFactorSessionToken(int userId, string email, string twoFactorType);

        ClaimsPrincipal? ValidateTwoFactorSessionToken(string token);

        string GenerateOAuthToken(string clientId, IEnumerable<string> scopes, IEnumerable<string> audiences, int expirySeconds = 1800);

        ClaimsPrincipal? ValidateToken(string token);
    }
}