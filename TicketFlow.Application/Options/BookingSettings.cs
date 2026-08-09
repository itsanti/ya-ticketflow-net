namespace TicketFlow.Application.Options
{
    public class BookingSettings
    {
        public const string SectionName = "Booking";

        public int MaxActiveBookingsPerUser { get; set; } = 10;
    }
}
