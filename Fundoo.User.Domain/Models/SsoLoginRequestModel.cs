using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Domain.Models
{
    public class SsoLoginRequestModel
    {
        [Required]
        public string Provider { get; set; } = "Google"; // e.g. "Google", "GitHub", "Microsoft"

        public string? IdToken { get; set; }

        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
