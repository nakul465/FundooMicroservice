using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Domain.Models
{
    public class TwoFactorSendEmailOtpRequestModel
    {
        [Required]
        public string TwoFactorSessionToken { get; set; } = string.Empty;
    }
}
