using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Domain.Models
{
    public class TwoFactorVerifyLoginRequestModel
    {
        [Required]
        public string TwoFactorSessionToken { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
