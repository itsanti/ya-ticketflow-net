using TicketFlow.DataAccess.Repositories;
using TicketFlow.Exceptions;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public class BookingService(
        IEventRepository eventRepo,
        IBookingRepository bookingRepo
        ) : IBookingService
    {
        private readonly IEventRepository _eventRepo = eventRepo;
        private readonly IBookingRepository _bookingRepo = bookingRepo;

        private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public async Task<Booking> CreateBookingAsync(Guid eventId)
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

                return booking;
            }
            finally
            {
                _bookingSemaphore.Release();
            }
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsNoTrackingAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
            }
            return booking;
        }
    }
}
