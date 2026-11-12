using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Domain.Models
{
    public class TwoFactorDisableRequestModel
    {
        public string? Password { get; set; }

        public string? Code { get; set; }
    }
}
