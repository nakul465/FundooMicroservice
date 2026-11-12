using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.User.Application.Services;
using Microsoft.Extensions.Configuration;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class OAuthTokenService : IOAuthTokenService
    {
        private readonly IJwtService _jwtService;
        private readonly IOAuthClientStore _clientStore;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> TokenCache = new();
        private static readonly SemaphoreSlim Semaphore = new(1, 1);

        public OAuthTokenService(
            IJwtService jwtService,
            IOAuthClientStore clientStore,
            IConfiguration configuration)
        {
            _jwtService = jwtService;
            _clientStore = clientStore;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync(string scope, string? audience = null)
        {
            var cacheKey = $"{scope}:{audience ?? "default"}";

            if (TokenCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.Expiry.AddMinutes(-2))
            {
                return cached.Token;
            }

            await Semaphore.WaitAsync();
            try
            {
                if (TokenCache.TryGetValue(cacheKey, out cached) && DateTime.UtcNow < cached.Expiry.AddMinutes(-2))
                {
                    return cached.Token;
                }

                var clientId = "user-service";
                var client = await _clientStore.GetClientAsync(clientId);
                if (client == null)
                {
                    throw new InvalidOperationException($"OAuth client '{clientId}' not registered.");
                }

                var requestedScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var audiences = new List<string>();
                if (!string.IsNullOrEmpty(audience))
                {
                    audiences.Add(audience);
                }
                else
                {
                    audiences.AddRange(client.AllowedAudiences);
                }

                const int expirySeconds = 1800; // 30 minutes
                var token = _jwtService.GenerateOAuthToken(clientId, requestedScopes, audiences, expirySeconds);

                TokenCache[cacheKey] = (token, DateTime.UtcNow.AddSeconds(expirySeconds));
                return token;
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
