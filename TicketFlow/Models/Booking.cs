namespace TicketFlow.Models
{
    public class Booking(Guid eventId)
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EventId { get; set; } = eventId;

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

    }
}
