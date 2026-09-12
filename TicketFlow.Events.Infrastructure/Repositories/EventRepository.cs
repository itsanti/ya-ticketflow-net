using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TicketFlow.Events.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly EventsDbContext _context;

        public EventRepository(EventsDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Event eventItem, CancellationToken ct = default)
        {
            await _context.Events.AddAsync(eventItem, ct);
        }

        public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task<IReadOnlyList<Event>> GetTopPopularAsync(int count, CancellationToken ct = default)
        {
            return await _context.Events
                .AsNoTracking()
                .OrderByDescending(e => (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
                .ThenBy(e => e.Id)
                .Take(count)
                .ToListAsync(ct);
        }

        public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetPagedAsync(EventFiltersDto filters, CancellationToken ct = default)
        {
            IQueryable<Event> query = _context.Events.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filters.Title))
            {
                var title = filters.Title.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(title));
            }

            if (filters.From.HasValue)
            {
                query = query.Where(e => e.StartAt >= filters.From.Value);
            }

            if (filters.To.HasValue)
            {
                query = query.Where(e => e.EndAt <= filters.To.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(e => e.StartAt)
                .ThenBy(e => e.Id)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public void Remove(Event eventItem)
        {
            _context.Events.Remove(eventItem);
        }

        public async Task<bool> IsBookingProcessedAsync(Guid bookingId, CancellationToken ct = default)
        {
            return await _context.ProcessedBookingConfirmations
                .AnyAsync(p => p.BookingId == bookingId, ct);
        }

        public async Task MarkBookingProcessedAsync(Guid bookingId, DateTime processedAtUtc, CancellationToken ct = default)
        {
            await _context.ProcessedBookingConfirmations.AddAsync(
                ProcessedBookingConfirmation.Create(bookingId, processedAtUtc), ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
