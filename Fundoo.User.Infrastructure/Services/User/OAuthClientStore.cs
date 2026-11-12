using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class OAuthClientStore : IOAuthClientStore
    {
        private readonly Dictionary<string, OAuthClient> _clients = new(StringComparer.OrdinalIgnoreCase);

        public OAuthClientStore(IConfiguration configuration)
        {
            // Register standard microservice clients (can be extended via appsettings)
            var userSecret = configuration["OAuth:Clients:UserService:Secret"] ?? "UserSvcSecret_2026!#";
            var gatewaySecret = configuration["OAuth:Clients:GatewayService:Secret"] ?? "GatewaySvcSecret_2026!#";
            var notifySecret = configuration["OAuth:Clients:NotificationService:Secret"] ?? "NotifySvcSecret_2026!#";

            _clients["user-service"] = new OAuthClient
            {
                ClientId = "user-service",
                ClientSecret = userSecret,
                ClientName = "Fundoo User Microservice",
                AllowedScopes = new List<string> { "notification:send", "notes:read", "notes:write", "notes:internal", "user:read" },
                AllowedAudiences = new List<string> { "FundooNotificationApi", "FundooNotesApi", "FundooResources" }
            };

            _clients["gateway-service"] = new OAuthClient
            {
                ClientId = "gateway-service",
                ClientSecret = gatewaySecret,
                ClientName = "Fundoo API Gateway",
                AllowedScopes = new List<string> { "notes:read", "notes:write", "notification:send", "user:read" },
                AllowedAudiences = new List<string> { "FundooNotesApi", "FundooNotificationApi", "FundooResources" }
            };

            _clients["notification-service"] = new OAuthClient
            {
                ClientId = "notification-service",
                ClientSecret = notifySecret,
                ClientName = "Fundoo Notification Service",
                AllowedScopes = new List<string> { "user:read" },
                AllowedAudiences = new List<string> { "FundooUserApi", "FundooResources" }
            };
        }

        public Task<OAuthClient?> GetClientAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return Task.FromResult<OAuthClient?>(null);
            _clients.TryGetValue(clientId, out var client);
            return Task.FromResult(client);
        }

        public Task<bool> ValidateCredentialsAsync(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return Task.FromResult(false);
            }

            if (_clients.TryGetValue(clientId, out var client))
            {
                var isValid = client.ClientSecret.Equals(clientSecret, StringComparison.Ordinal);
                return Task.FromResult(isValid);
            }

            return Task.FromResult(false);
        }
    }
}
