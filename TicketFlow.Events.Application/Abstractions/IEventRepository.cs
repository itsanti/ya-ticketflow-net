using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Application.Abstractions
{
    public interface IEventRepository
    {
        Task<(IReadOnlyList<Event> Items, int TotalCount)> GetPagedAsync(EventFiltersDto filters, CancellationToken ct = default);
        Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Event eventItem, CancellationToken ct = default);
        void Remove(Event eventItem);
        Task<bool> IsBookingProcessedAsync(Guid bookingId, CancellationToken ct = default);
        Task MarkBookingProcessedAsync(Guid bookingId, DateTime processedAtUtc, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
