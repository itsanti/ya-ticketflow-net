using Microsoft.Extensions.Options;
using TicketFlow.Application.Abstractions;
using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Application.Options;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Application.Services
{
    public class BookingService(
        IEventRepository eventRepo,
        IBookingRepository bookingRepo,
        IOptions<BookingSettings> bookingSettings
        ) : IBookingService
    {
        private readonly IEventRepository _eventRepo = eventRepo;
        private readonly IBookingRepository _bookingRepo = bookingRepo;
        private readonly int _maxActiveBookingsPerUser = bookingSettings.Value.MaxActiveBookingsPerUser;

        private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId)
        {
            await _bookingSemaphore.WaitAsync();
            try
            {
                var eventItem = await _eventRepo.GetByIdAsync(eventId);

                if (eventItem == null)
                {
                    throw new NotFoundException($"Cannot create booking. Event with ID {eventId} not found.");
                }

                if (eventItem.StartAt <= DateTime.UtcNow)
                {
                    throw new EventAlreadyStartedException($"Event with ID {eventId} already started.");
                }

                int count = await _bookingRepo.CountActiveBookingsByUserAsync(userId);

                if (count >= _maxActiveBookingsPerUser)
                {
                    throw new BookingLimitExceededException(
                        $"Booking limit exceeded: {_maxActiveBookingsPerUser} active bookings per user.");
                }

                bool ok = eventItem.TryReserveSeats();
                if (!ok)
                {
                    throw new NoAvailableSeatsException($"Cannot create booking. No available seats for event with ID {eventId}.");
                }

                var booking = new Booking(eventId, userId);

                await _bookingRepo.AddAsync(booking);
                await _bookingRepo.SaveChangesAsync();

                return MapToDto(booking);
            }
            finally
            {
                _bookingSemaphore.Release();
            }
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

            var eventItem = await _eventRepo.GetByIdAsync(booking.EventId);

            if (eventItem == null)
            {
                throw new NotFoundException($"Event with ID {booking.EventId} not found.");
            }

            if (eventItem.StartAt <= DateTime.UtcNow)
            {
                throw new EventAlreadyStartedException($"Event with ID {booking.EventId} already started.");
            }

            booking.Cancel();
            await _bookingRepo.SaveChangesAsync();
        }
    }
}
