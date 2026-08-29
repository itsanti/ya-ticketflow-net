using TicketFlow.Contracts;

namespace TicketFlow.Bookings.Application.Abstractions
{
    public interface IBookingConfirmedPublisher
    {
        Task PublishAsync(BookingConfirmedEvent bookingConfirmedEvent, CancellationToken ct = default);
    }
}
