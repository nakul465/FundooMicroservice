using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Fundoo.User.Application.Services;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class NotesServiceClient : INotesServiceClient
    {
        private readonly HttpClient _httpClient;

        public NotesServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> PingNotesServiceAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health/live");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetUserNotesCountAsync(int userId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/notes/all");
                request.Headers.Add("X-User-Id", userId.ToString());
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return 0;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var jsonDoc = await JsonDocument.ParseAsync(stream);
                if (jsonDoc.RootElement.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                {
                    return dataProp.GetArrayLength();
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
