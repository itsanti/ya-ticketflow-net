using TicketFlow.Bookings.Application.DTOs;
using TicketFlow.Bookings.Domain.Enums;

namespace TicketFlow.Bookings.Application.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId);

        Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, Guid userId, UserRole role);

        Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role);
    }
}
