using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;

        public EmailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EmailResponseModel> SendEmailAsync(EmailRequestModel request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/notification/send", request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<EmailResponseModel>()
                   ?? throw new InvalidOperationException("Notification service returned an empty response.");
        }
    }
}
