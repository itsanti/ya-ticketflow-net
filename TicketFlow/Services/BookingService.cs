using TicketFlow.Exceptions;
using TicketFlow.Models;
using TicketFlow.Models.Store;

namespace TicketFlow.Services
{
    public class BookingService(
        IInMemoryStore<Booking> store,
        IInMemoryStore<Event> eventStore
        ) : IBookingService
    {
        private readonly IInMemoryStore<Booking> _bookingStore = store;
        private readonly IInMemoryStore<Event> _eventStore = eventStore;

        private readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            await _bookingSemaphore.WaitAsync();
            try
            {
                var events = await _eventStore.FindAsync(e => e.Id == eventId);
                var eventItem = events.FirstOrDefault();

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

                await _eventStore.UpdateAsync(eventItem);
                await _bookingStore.AddAsync(booking);

                return booking;
            }
            finally
            {
                _bookingSemaphore.Release();
            }
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
        {
            var bookings = await _bookingStore.FindAsync(b => b.Id == bookingId);
            var booking = bookings.FirstOrDefault();
            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
            }
            return booking;
        }
    }
}
