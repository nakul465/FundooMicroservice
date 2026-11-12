using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Domain.Models
{
    public class TwoFactorEnableRequestModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        public string TwoFactorType { get; set; } = "TOTP"; // "TOTP" or "Email"
    }
}
