namespace Fundoo.User.Domain.Models
{
    public class SsoUserInfoModel
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string Provider { get; set; } = string.Empty;
    }
}
