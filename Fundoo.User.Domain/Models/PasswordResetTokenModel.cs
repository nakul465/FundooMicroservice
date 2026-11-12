using System;
namespace Fundoo.User.Domain.Models
{
	public class PasswordResetTokenModel
	{
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; }
    }
}

