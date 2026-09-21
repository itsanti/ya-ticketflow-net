namespace TicketFlow.Events.Presentation.Options
{
    public class OtlpOptions
    {
        public const string SectionName = "Otlp";

        public string ServiceName { get; set; } = "events-service";

        public string Endpoint { get; set; } = string.Empty;
    }
}
