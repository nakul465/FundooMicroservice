using System.Text.Json.Serialization;

namespace Fundoo.User.Domain.Models
{
    public class OAuthTokenResponseModel
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 1800; // 30 minutes

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }
}
