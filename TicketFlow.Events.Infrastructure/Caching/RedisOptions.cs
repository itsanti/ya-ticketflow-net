namespace TicketFlow.Events.Infrastructure.Caching
{
    public class RedisOptions
    {
        public const string SectionName = "Redis";

        public required string ConnectionString { get; set; }
    }
}
