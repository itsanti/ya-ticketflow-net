namespace TicketFlow.Users.Presentation.Options
{
    public class OtlpOptions
    {
        public const string SectionName = "Otlp";

        public string ServiceName { get; set; } = "users-service";

        public string Endpoint { get; set; } = string.Empty;
    }
}
