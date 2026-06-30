using Microsoft.EntityFrameworkCore;
using TicketFlow.Models;

namespace TicketFlow.DataAccess.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Booking booking, CancellationToken ct = default)
        {
            await _context.Bookings.AddAsync(booking, ct);
        }

        public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct = default)
        {
            return await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        }

        public async Task<Booking?> GetByIdAsNoTrackingAsync(Guid bookingId, CancellationToken ct = default)
        {
            return await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        }

        public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(CancellationToken ct = default)
        {
            return await _context.Bookings
                            .AsNoTracking()
                            .Where(b => b.Status == BookingStatus.Pending)
                            .Select(b => b.Id)
                            .ToListAsync(ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
