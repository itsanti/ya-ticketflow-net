using TicketFlow.Application.DTOs.Events;
using TicketFlow.Application.DTOs.Pagination;

namespace TicketFlow.Application.Services
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
