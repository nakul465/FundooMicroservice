using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fundoo.Notification.Domain.Models;
using Fundoo.Notification.Infrastructure.Email.Template;
using Fundoo.Notification.Application.Feature.Notification.Services;

namespace Fundoo.Notification.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task<EmailResponseModel> SendEmailAsync(EmailRequestModel request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
                {
                    var msg = "SMTP Host is not configured in SmtpSettings.";
                    _logger.LogError(msg);
                    return new EmailResponseModel { Success = false, Message = msg };
                }

                if (string.IsNullOrWhiteSpace(_smtpSettings.Username))
                {
                    var msg = "SMTP Username is not configured in SmtpSettings.";
                    _logger.LogError(msg);
                    return new EmailResponseModel { Success = false, Message = msg };
                }

                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
                client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                client.EnableSsl = _smtpSettings.EnableSsl;

                var mail = new MailMessage(
                    _smtpSettings.Username,
                    request.To,
                    request.Subject,
                    request.Body);

                mail.IsBodyHtml = request.IsHtml;

                _logger.LogInformation("Attempting to send email via SMTP to {Recipient} (Host: {Host}:{Port}, User: {Username})...", 
                    request.To, _smtpSettings.Host, _smtpSettings.Port, _smtpSettings.Username);
                await client.SendMailAsync(mail);
                _logger.LogInformation("SMTP email successfully sent to {Recipient}!", request.To);

                return new EmailResponseModel
                {
                    Success = true,
                    Message = "Email sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP to {Recipient}: {Error}", request.To, ex.Message);
                return new EmailResponseModel
                {
                    Success = false,
                    Message = $"SMTP sending failed: {ex.Message}"
                };
            }
        }
    }
}
