using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Application.DTOs.Pagination;

namespace TicketFlow.Events.Application.Services
{
    public interface IEventService
    {
        Task<PaginatedResult<EventInfoDto>> GetEventsAsync(EventFiltersDto filters);
        Task<EventInfoDto> GetEventAsync(Guid eventId);
        Task<Guid> AddEventAsync(CreateEventDto dto);
        Task<EventInfoDto> UpdateEventAsync(Guid eventId, UpdateEventDto dto);
        Task RemoveEventAsync(Guid eventId);
    }
}
