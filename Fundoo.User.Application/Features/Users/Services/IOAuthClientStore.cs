using System.Threading.Tasks;
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Application.Services
{
    public interface IOAuthClientStore
    {
        Task<OAuthClient?> GetClientAsync(string clientId);
        Task<bool> ValidateCredentialsAsync(string clientId, string clientSecret);
    }
}
