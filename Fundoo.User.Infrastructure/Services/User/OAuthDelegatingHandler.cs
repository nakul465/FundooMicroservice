using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Fundoo.User.Application.Services;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class OAuthDelegatingHandler : DelegatingHandler
    {
        private readonly IOAuthTokenService _tokenService;
        private readonly string _scope;
        private readonly string? _audience;

        public OAuthDelegatingHandler(
            IOAuthTokenService tokenService,
            string scope = "notification:send notes:read notes:write notes:internal",
            string? audience = null)
        {
            _tokenService = tokenService;
            _scope = scope;
            _audience = audience;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null || string.IsNullOrWhiteSpace(request.Headers.Authorization.Parameter))
            {
                var token = await _tokenService.GetAccessTokenAsync(_scope, _audience);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Retry once with a forced fresh token if unauthorized
                var freshToken = await _tokenService.GetAccessTokenAsync(_scope, _audience);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
                response = await base.SendAsync(request, cancellationToken);
            }

            return response;
        }
    }
}
