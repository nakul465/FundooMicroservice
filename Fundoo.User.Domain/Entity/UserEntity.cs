using System;
using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Domain.Entity
{
	public class UserEntity
	{
        [Key]
        public int UserId { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        // 2FA Properties
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }
        public string? TwoFactorType { get; set; } // "TOTP" or "Email"

        // SSO Properties
        public string? SsoProvider { get; set; } // "Google", "GitHub", "Microsoft", "Local"
        public string? SsoProviderId { get; set; }
    }
}

