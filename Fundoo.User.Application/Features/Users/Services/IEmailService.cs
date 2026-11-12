
using Fundoo.User.Domain.Models;

namespace Fundoo.User.Application.Services
{
	public interface IEmailService
	{
        public Task<EmailResponseModel> SendEmailAsync(EmailRequestModel request);

    }
}

