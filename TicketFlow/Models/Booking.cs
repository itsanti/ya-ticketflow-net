namespace TicketFlow.Models
{
    public class Booking(Guid eventId)
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EventId { get; set; } = eventId;

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

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
