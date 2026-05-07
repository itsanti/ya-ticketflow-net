using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public interface IEventService
    {
        PaginatedResult<Event> GetEvents(EventFiltersDto filters);
        Event? GetEvent(Guid eventId);
        Guid AddEvent(CreateEventDto dto);
        Event? UpdateEvent(Guid eventId, UpdateEventDto dto);
        bool RemoveEvent(Guid eventId);
    }
}
