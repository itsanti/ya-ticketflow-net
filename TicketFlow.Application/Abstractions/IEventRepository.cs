using TicketFlow.Application.DTOs.Events;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions
{
    public interface IEventRepository
    {
        Task<(IReadOnlyList<Event> Items, int TotalCount)> GetPagedAsync(EventFiltersDto filters, CancellationToken ct = default);
        Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Event eventItem, CancellationToken ct = default);
        void Remove(Event eventItem);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
