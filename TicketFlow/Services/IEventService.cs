using TicketFlow.DTOs.Events;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public interface IEventService
    {
        List<Event> GetEvents();
        Event? GetEvent(Guid eventId);
        Guid AddEvent(CreateEventDto dto);
        Event? UpdateEvent(Guid eventId, UpdateEventDto dto);
        void RemoveEvent(Guid eventId);
    }
}
