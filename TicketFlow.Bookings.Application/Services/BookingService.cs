using Microsoft.Extensions.Options;
using TicketFlow.Bookings.Application.Abstractions;
using TicketFlow.Bookings.Application.DTOs;
using TicketFlow.Bookings.Application.Options;
using TicketFlow.Bookings.Domain.Entities;
using TicketFlow.Bookings.Domain.Enums;
using TicketFlow.Bookings.Domain.Exceptions;

namespace TicketFlow.Bookings.Application.Services
{
    // Нет IEventRepository: у Bookings своей БД для events нет, проверки по событию
    // (существование/старт/места) переехали в Events через Kafka.
    public class BookingService(
        IBookingRepository bookingRepo,
        IOptions<BookingSettings> bookingSettings
        ) : IBookingService
    {
        private readonly IBookingRepository _bookingRepo = bookingRepo;
        private readonly int _maxActiveBookingsPerUser = bookingSettings.Value.MaxActiveBookingsPerUser;

        public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId)
        {
            int count = await _bookingRepo.CountActiveBookingsByUserAsync(userId);

            if (count >= _maxActiveBookingsPerUser)
            {
                throw new BookingLimitExceededException(
                    $"Booking limit exceeded: {_maxActiveBookingsPerUser} active bookings per user.");
            }

            var booking = new Booking(eventId, userId);

            await _bookingRepo.AddAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            return MapToDto(booking);
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, Guid userId, UserRole role)
        {
            var booking = await _bookingRepo.GetByIdAsNoTrackingAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
            }

            if (booking.UserId != userId && role != UserRole.Admin)
            {
                throw new ForbiddenException("You can not view other user booking.");
            }

            return MapToDto(booking);
        }

        private static BookingResponseDto MapToDto(Booking booking)
        {
            return new BookingResponseDto
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };
        }

        public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new NotFoundException($"Cannot find booking with ID {bookingId}.");
            }

            if (booking.UserId != userId && role != UserRole.Admin)
            {
                throw new ForbiddenException("You can not cancel other user booking.");
            }

            booking.Cancel();
            await _bookingRepo.SaveChangesAsync();
        }
    }
}
