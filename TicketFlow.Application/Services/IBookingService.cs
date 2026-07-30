using TicketFlow.Application.DTOs.Bookings;

namespace TicketFlow.Application.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(Guid eventId);

        Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId);
    }
}
