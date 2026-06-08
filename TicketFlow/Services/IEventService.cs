using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;

namespace TicketFlow.Services
{
    public interface IEventService
    {
        Task<PaginatedResult<EventInfoDto>> GetEventsAsync(EventFiltersDto filters);
        Task<EventInfoDto> GetEventAsync(Guid eventId);
        Task<Guid> AddEventAsync(CreateEventDto dto);
        Task<EventInfoDto> UpdateEventAsync(Guid eventId, UpdateEventDto dto);
        Task<bool> RemoveEventAsync(Guid eventId);
    }
}
