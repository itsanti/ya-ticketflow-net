using Microsoft.EntityFrameworkCore;
using TicketFlow.DTOs.Events;
using TicketFlow.Models;

namespace TicketFlow.DataAccess.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
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

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
