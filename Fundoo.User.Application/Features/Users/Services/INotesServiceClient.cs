using System.Threading.Tasks;

namespace Fundoo.User.Application.Services
{
    public interface INotesServiceClient
    {
        Task<bool> PingNotesServiceAsync();
        Task<int> GetUserNotesCountAsync(int userId);
    }
}
