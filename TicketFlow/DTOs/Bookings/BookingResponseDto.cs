namespace TicketFlow.DTOs.Bookings
{
    public class BookingResponseDto
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public string Status { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
