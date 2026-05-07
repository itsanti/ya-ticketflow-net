using TicketFlow.DTOs.Events;
using TicketFlow.Exceptions;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = [];

        public List<Event> GetEvents(EventFiltersDto filters)
        {
            IEnumerable<Event> query = _events;

            if (!string.IsNullOrWhiteSpace(filters.Title))
            {
                query = query.Where(e => e.Title.Contains(filters.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (filters.From.HasValue)
            {
                query = query.Where(e => e.StartAt >= filters.From.Value);
            }

            if (filters.To.HasValue)
            {
                query = query.Where(e => e.EndAt <= filters.To.Value);
            }

            return [.. query];
        }

        public Event GetEvent(Guid eventId)
        {
            var eventItem = _events.FirstOrDefault(e => e.Id == eventId);
            if (eventItem == null)
            {
                throw new NotFoundException($"Event with ID {eventId} not found.");
            }
            return eventItem;
        }

        public Guid AddEvent(CreateEventDto dto)
        {
            ValidateDates(dto.StartAt, dto.EndAt);

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

        public Event UpdateEvent(Guid eventId, UpdateEventDto dto)
        {
            ValidateDates(dto.StartAt, dto.EndAt);

            var existingEvent = GetEvent(eventId);

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartAt = dto.StartAt;
            existingEvent.EndAt = dto.EndAt;

            return existingEvent;
        }

        public bool RemoveEvent(Guid eventId)
        {
            var eventToRemove = GetEvent(eventId);
            return _events.Remove(eventToRemove);
        }

        private static void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (endAt <= startAt)
                throw new ValidationException("EndAt must be greater than StartAt");
        }
    }
}