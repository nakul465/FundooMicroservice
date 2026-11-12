using System.Text.Json.Serialization;

namespace Fundoo.User.Domain.Models
{
    public class OAuthTokenRequestModel
    {
        [JsonPropertyName("grant_type")]
        public string? GrantType { get; set; } = "client_credentials";

        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
