using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Fundoo.Notification.Application.Feature.Notification.Services;
using Fundoo.Notification.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fundoo.Notification.Infrastructure.Services
{
    public class KafkaEmailConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaEmailConsumerService> _logger;

        public KafkaEmailConsumerService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<KafkaEmailConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"]
                ?? _configuration.GetConnectionString("Kafka")
                ?? "kafka:9092";

            var topic = _configuration["Kafka:Topic"] ?? "email-notifications";
            var groupId = _configuration["Kafka:GroupId"] ?? "fundoo-notification-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                SocketTimeoutMs = 10000,
                SessionTimeoutMs = 30000
            };

            _logger.LogInformation("Starting Kafka Email Consumer for servers: {Servers}, topic: {Topic}, groupId: {GroupId}",
                bootstrapServers, topic, groupId);

            return Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<string, string>(config)
                    .SetErrorHandler((_, e) => _logger.LogWarning("Kafka Consumer error: {Reason}", e.Reason))
                    .SetLogHandler((_, msg) => _logger.LogInformation("Kafka [{Facility}]: {Message}", msg.Facility, msg.Message))
                    .SetPartitionsAssignedHandler((c, partitions) =>
                    {
                        _logger.LogInformation("Kafka Consumer assigned partitions: [{Partitions}]",
                            string.Join(", ", partitions.Select(p => $"{p.Topic}:{p.Partition.Value}")));
                    })
                    .SetPartitionsRevokedHandler((c, partitions) =>
                    {
                        _logger.LogInformation("Kafka Consumer revoked partitions: [{Partitions}]",
                            string.Join(", ", partitions.Select(p => $"{p.Topic}:{p.Partition.Value}")));
                    })
                    .Build();

                try
                {
                    consumer.Subscribe(topic);
                    _logger.LogInformation("Subscribed to topic '{Topic}'. Waiting for messages on partition...", topic);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);
                            if (consumeResult == null || consumeResult.IsPartitionEOF)
                            {
                                continue;
                            }

                            _logger.LogInformation(">>> Received Kafka email message at offset {Offset} for recipient '{Key}'! Body length: {Length}",
                                consumeResult.Offset, consumeResult.Message.Key, consumeResult.Message.Value?.Length ?? 0);

                            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var emailRequest = JsonSerializer.Deserialize<EmailRequestModel>(consumeResult.Message.Value, jsonOptions);
                            if (emailRequest == null || string.IsNullOrWhiteSpace(emailRequest.To))
                            {
                                _logger.LogWarning("Received invalid or empty email request payload. Skipping and committing offset.");
                                consumer.Commit(consumeResult);
                                continue;
                            }

                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                _logger.LogInformation("Dispatching email for '{Recipient}' to EmailService...", emailRequest.To);
                                var result = await emailService.SendEmailAsync(emailRequest);

                                if (result.Success)
                                {
                                    _logger.LogInformation("Successfully sent email to '{Recipient}' from Kafka message.", emailRequest.To);
                                }
                                else
                                {
                                    _logger.LogWarning("Email delivery failed for '{Recipient}': {Message}", emailRequest.To, result.Message);
                                }
                            }

                            // Commit offset only after processing
                            consumer.Commit(consumeResult);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogWarning("Kafka consume exception: {Reason}. Retrying in 3 seconds...", ex.Error.Reason);
                            await Task.Delay(3000, stoppingToken);
                        }
                        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogError(ex, "Unexpected error processing Kafka email message. Retrying in 3 seconds...");
                            await Task.Delay(3000, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka Email Consumer stopping due to cancellation request.");
                }
                finally
                {
                    try
                    {
                        consumer.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while closing Kafka consumer.");
                    }
                }
            }, stoppingToken);
        }
    }
}
