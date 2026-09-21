namespace TicketFlow.Bookings.Presentation.Options
{
    public class OtlpOptions
    {
        public const string SectionName = "Otlp";

        public string ServiceName { get; set; } = "bookings-service";

        public string Endpoint { get; set; } = string.Empty;
    }
}
