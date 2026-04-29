using TicketFlow.Models;

namespace TicketFlow.Services
{
    public interface IEventService
    {
        List<Event> GetEvents();
        Event? GetEvent(Guid eventId);
        Guid AddEvent(Event newEvent);
        Event? UpdateEvent(Event updatedEvent);
        void RemoveEvent(Guid eventId);
    }
}
