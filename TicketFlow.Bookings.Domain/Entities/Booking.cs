using TicketFlow.Bookings.Domain.Enums;
using TicketFlow.Bookings.Domain.Exceptions;

namespace TicketFlow.Bookings.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Ссылки на Event/User из других сервисов — только по Id, без nav-свойств.
        public Guid EventId { get; private set; }

        public Guid UserId { get; private set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        private Booking()
        {
        }

        public Booking(Guid eventId, Guid userId)
        {
            EventId = eventId;
            UserId = userId;
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

        public void Cancel()
        {
            if (Status is BookingStatus.Cancelled or BookingStatus.Rejected)
            {
                throw new InvalidOperationDomainException("Booking cannot be cancelled in its current status");
            }

            Status = BookingStatus.Cancelled;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}
