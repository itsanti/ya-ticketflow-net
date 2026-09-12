namespace TicketFlow.Events.Application.Caching
{
    public static class CacheKeys
    {
        public const string TopEventsKey = "events:top10";

        public static string EventKey(Guid eventId) => $"event:{eventId}";
    }
}
