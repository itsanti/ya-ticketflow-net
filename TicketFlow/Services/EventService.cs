using TicketFlow.Models;

namespace TicketFlow.Services
{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = [];

        public List<Event> GetEvents()
        {
            return [.. _events];
        }

        public Event? GetEvent(Guid eventId)
        {
            return _events.FirstOrDefault(e => e.Id == eventId);
        }

        public Guid AddEvent(Event newEvent)
        {
            newEvent.Id = Guid.NewGuid();
            _events.Add(newEvent);
            return newEvent.Id;
        }

        public Event? UpdateEvent(Event updatedEvent)
        {
            var existingEvent = GetEvent(updatedEvent.Id);
            if (existingEvent == null)
                return null;

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartAt = updatedEvent.StartAt;
            existingEvent.EndAt = updatedEvent.EndAt;

            return existingEvent;
        }

        public void RemoveEvent(Guid eventId)
        {
            var eventToRemove = GetEvent(eventId);
            if (eventToRemove != null)
                _events.Remove(eventToRemove);
        }
    }
}