using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Abstractions
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Booking?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Booking booking, CancellationToken ct = default);
        Task<IReadOnlyList<Guid>> GetPendingIdsAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<int> CountActiveBookingsByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
