namespace TicketFlow.Bookings.Infrastructure.Messaging
{
    public class KafkaOptions
    {
        public const string SectionName = "Kafka";

        public required string BootstrapServers { get; set; }
    }
}
