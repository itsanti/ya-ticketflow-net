using TicketFlow.Exceptions;
using TicketFlow.Models;
using TicketFlow.Models.Store;

namespace TicketFlow.Services
{
    public class BookingService(IInMemoryStore<Booking> store) : IBookingService
    {
        private readonly IInMemoryStore<Booking> _store = store;

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            var booking = new Booking(eventId);
            await _store.AddAsync(booking);
            return booking;
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
        {
            var bookings = await _store.FindAsync(b => b.Id == bookingId);
            var booking = bookings.FirstOrDefault();
            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
            }
            return booking;
        }
    }
}
