using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId);

        Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId);

        Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role);
    }
}
