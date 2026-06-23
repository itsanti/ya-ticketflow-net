using Microsoft.EntityFrameworkCore;
using TicketFlow.DataAccess;
using TicketFlow.Exceptions;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public class BookingService(
        AppDbContext context
        ) : IBookingService
    {
        private readonly AppDbContext _context = context;

        private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            await _bookingSemaphore.WaitAsync();
            try
            {
                var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

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

                await _context.Bookings.AddAsync(booking);
                await _context.SaveChangesAsync();

                return booking;
            }
            finally
            {
                _bookingSemaphore.Release();
            }
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} not found.");
            }
            return booking;
        }
    }
}
