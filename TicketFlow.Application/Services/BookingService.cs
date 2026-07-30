using TicketFlow.Application.Abstractions;
using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Application.Services
{
    public class BookingService(
        IEventRepository eventRepo,
        IBookingRepository bookingRepo
        ) : IBookingService
    {
        private readonly IEventRepository _eventRepo = eventRepo;
        private readonly IBookingRepository _bookingRepo = bookingRepo;

        private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
        {
            await _bookingSemaphore.WaitAsync();
            try
            {
                var eventItem = await _eventRepo.GetByIdAsync(eventId);

                if (eventItem == null)
                {
                    throw new NotFoundException($"Cannot create booking. Event with ID {eventId} not found.");
                }

                bool ok = eventItem.TryReserveSeats();
                if (!ok)
                {
                    throw new NoAvailableSeatsException($"Cannot create booking. No available seats for event with ID {eventId}.");
                }

                var booking = new Booking(eventId);

                await _bookingRepo.AddAsync(booking);
                await _bookingRepo.SaveChangesAsync();

                return MapToDto(booking);
            }
            finally
            {
                _bookingSemaphore.Release();
            }
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsNoTrackingAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
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
    }
}
