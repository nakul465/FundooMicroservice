using System.Threading.Tasks;

namespace Fundoo.User.Application.Services
{
    public interface IOAuthTokenService
    {
        Task<string> GetAccessTokenAsync(string scope, string? audience = null);
    }
}
