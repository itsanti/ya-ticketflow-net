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

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            var events = await _eventStore.FindAsync(e => e.Id == eventId);
            var eventItem = events.FirstOrDefault();

            if (eventItem == null)
            {
                throw new NotFoundException($"Cannot create booking. Event with ID {eventId} not found.");
            }

            var booking = new Booking(eventId);
            await _bookingStore.AddAsync(booking);
            return booking;
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
