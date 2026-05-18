using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Exceptions;
using TicketFlow.Models;
using TicketFlow.Models.Store;

namespace TicketFlow.Services
{
    public class EventService : IEventService
    {
        private readonly IInMemoryStore<Event> _eventStore;

        public EventService(IInMemoryStore<Event> eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<PaginatedResult<Event>> GetEventsAsync(EventFiltersDto filters)
        {
            var allEvents = await _eventStore.GetAllAsync();
            IEnumerable<Event> query = allEvents;

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

            var totalCount = query.Count();

            var items = query
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            return new PaginatedResult<Event>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filters.Page,
                PageSize = items.Count
            };
        }

        public async Task<Event> GetEventAsync(Guid eventId)
        {
            var events = await _eventStore.FindAsync(e => e.Id == eventId);
            var eventItem = events.FirstOrDefault();

            if (eventItem == null)
            {
                throw new NotFoundException($"Event with ID {eventId} not found.");
            }
            return eventItem;
        }

        public async Task<Guid> AddEventAsync(CreateEventDto dto)
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
            await _eventStore.AddAsync(newEvent);
            return newEvent.Id;
        }

        public async Task<Event> UpdateEventAsync(Guid eventId, UpdateEventDto dto)
        {
            ValidateDates(dto.StartAt, dto.EndAt);

            var existingEvent = await GetEventAsync(eventId);

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartAt = dto.StartAt;
            existingEvent.EndAt = dto.EndAt;

            await _eventStore.UpdateAsync(existingEvent);
            return existingEvent;
        }

        public async Task<bool> RemoveEventAsync(Guid eventId)
        {
            await GetEventAsync(eventId);
            return await _eventStore.DeleteAsync(eventId);
        }

        private static void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (endAt <= startAt)
                throw new ValidationException("EndAt must be greater than StartAt");
        }
    }
}