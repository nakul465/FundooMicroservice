using System.Collections.Generic;

namespace Fundoo.User.Domain.Models
{
    public class OAuthClient
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public List<string> AllowedScopes { get; set; } = new();
        public List<string> AllowedAudiences { get; set; } = new();
    }
}
