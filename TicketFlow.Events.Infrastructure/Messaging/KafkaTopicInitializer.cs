using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketFlow.Contracts;

namespace TicketFlow.Events.Infrastructure.Messaging
{
    public class KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken ct)
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = options.Value.BootstrapServers
            }).Build();

            try
            {
                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = KafkaTopics.BookingConfirmed,
                        NumPartitions = 3,
                        ReplicationFactor = 1
                    }
                ]);

                logger.LogInformation("Kafka topic '{Topic}' created.", KafkaTopics.BookingConfirmed);
            }
            catch (CreateTopicsException ex) when (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
            {
                logger.LogInformation("Kafka topic '{Topic}' already exists.", KafkaTopics.BookingConfirmed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure Kafka topic '{Topic}' exists.", KafkaTopics.BookingConfirmed);
            }
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
