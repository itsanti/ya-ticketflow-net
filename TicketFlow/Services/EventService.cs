using TicketFlow.DTOs.Events;
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

        public Guid AddEvent(CreateEventDto dto)
        {
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                StartAt = dto.StartAt,
                EndAt = dto.EndAt
            };
            _events.Add(newEvent);
            return newEvent.Id;
        }

        public Event? UpdateEvent(Guid eventId, UpdateEventDto dto)
        {
            var existingEvent = GetEvent(eventId);
            if (existingEvent == null)
                return null;

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartAt = dto.StartAt;
            existingEvent.EndAt = dto.EndAt;

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