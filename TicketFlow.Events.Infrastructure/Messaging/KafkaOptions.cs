namespace TicketFlow.Events.Infrastructure.Messaging
{
    public class KafkaOptions
    {
        public const string SectionName = "Kafka";

        public required string BootstrapServers { get; set; }

        public required string ConsumerGroup { get; set; }
    }
}
