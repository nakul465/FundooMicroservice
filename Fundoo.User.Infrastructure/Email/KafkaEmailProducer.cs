using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fundoo.User.Infrastructure.Email
{
    public class KafkaEmailProducer : IEmailService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;
        private readonly ILogger<KafkaEmailProducer> _logger;
        private bool _disposed;

        public KafkaEmailProducer(IConfiguration configuration, ILogger<KafkaEmailProducer> logger)
        {
            _logger = logger;

            var bootstrapServers = configuration["Kafka:BootstrapServers"]
                ?? configuration.GetConnectionString("Kafka")
                ?? "kafka:9092";

            _topic = configuration["Kafka:Topic"] ?? "email-notifications";

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 5000,
                SocketTimeoutMs = 5000,
                RetryBackoffMs = 500
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger.LogInformation("KafkaEmailProducer initialized for servers: {Servers}, topic: {Topic}", bootstrapServers, _topic);
        }

        public async Task<EmailResponseModel> SendEmailAsync(EmailRequestModel request)
        {
            try
            {
                var payload = JsonSerializer.Serialize(request);
                var message = new Message<string, string>
                {
                    Key = request.To ?? string.Empty,
                    Value = payload
                };

                var deliveryResult = await _producer.ProduceAsync(_topic, message);
                _logger.LogInformation("Email notification queued to Kafka topic '{Topic}' at offset {Offset} for recipient '{Recipient}'.",
                    deliveryResult.Topic, deliveryResult.Offset, request.To);

                return new EmailResponseModel
                {
                    Success = true,
                    Message = "Email notification queued successfully via Kafka."
                };
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Kafka delivery failed for recipient '{Recipient}': {Reason}", request.To, ex.Error.Reason);
                return new EmailResponseModel
                {
                    Success = false,
                    Message = $"Kafka delivery failed: {ex.Error.Reason}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing email to Kafka for recipient '{Recipient}'.", request.To);
                return new EmailResponseModel
                {
                    Success = false,
                    Message = $"Failed to queue email notification: {ex.Message}"
                };
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
                _producer.Dispose();
                _disposed = true;
            }
        }
    }
}
