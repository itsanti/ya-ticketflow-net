using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public interface IEventService
    {
        Task<PaginatedResult<EventInfoDto>> GetEventsAsync(EventFiltersDto filters);
        Task<Event> GetEventAsync(Guid eventId);
        Task<Guid> AddEventAsync(CreateEventDto dto);
        Task<EventInfoDto> UpdateEventAsync(Guid eventId, UpdateEventDto dto);
        Task<bool> RemoveEventAsync(Guid eventId);
    }
}
