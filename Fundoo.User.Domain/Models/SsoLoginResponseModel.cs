namespace Fundoo.User.Domain.Models
{
    public class SsoLoginResponseModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool IsNewUser { get; set; }
        public bool RequiresTwoFactor { get; set; } = false;
        public string? TwoFactorSessionToken { get; set; }
        public string? TwoFactorType { get; set; }
    }
}
