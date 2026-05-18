using TicketFlow.Models;

namespace TicketFlow.Services
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId);

        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    }
}
