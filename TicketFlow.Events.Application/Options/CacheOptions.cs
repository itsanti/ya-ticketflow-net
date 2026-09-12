namespace TicketFlow.Events.Application.Options
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        public int EventTtlSeconds { get; set; } = 60;

        public int TopEventsTtlSeconds { get; set; } = 300;
    }
}
