namespace TicketFlow.Events.Domain.Entities
{
    public class ProcessedBookingConfirmation
    {
        public Guid BookingId { get; private set; }

        public DateTime ProcessedAtUtc { get; private set; }

        private ProcessedBookingConfirmation() { }

        public static ProcessedBookingConfirmation Create(Guid bookingId, DateTime processedAtUtc)
        {
            return new ProcessedBookingConfirmation
            {
                BookingId = bookingId,
                ProcessedAtUtc = processedAtUtc
            };
        }
    }
}
