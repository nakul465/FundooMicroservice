using System;
namespace Fundoo.User.Domain.Models
{
	public class UserResponseModel
	{
        public int UserId { get; set; }
        public string Fname { get; set; } = string.Empty;
        public string Lname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNo { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; } = false;
        public string? TwoFactorSessionToken { get; set; }
        public string? TwoFactorType { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? SsoProvider { get; set; }
    }
}

