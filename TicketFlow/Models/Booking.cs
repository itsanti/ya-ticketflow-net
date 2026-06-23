namespace TicketFlow.Models
{
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EventId { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public Event Event { get; private set; } = null!;

        private Booking()
        {
        }

        public Booking(Guid eventId)
        {
            EventId = eventId;
        }

        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}
