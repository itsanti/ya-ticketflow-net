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

        public async Task<PaginatedResult<EventInfoDto>> GetEventsAsync(EventFiltersDto filters)
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
                .Select(eventItem => new EventInfoDto
                {
                    Id = eventItem.Id,
                    Title = eventItem.Title,
                    Description = eventItem.Description,
                    StartAt = eventItem.StartAt,
                    EndAt = eventItem.EndAt,
                    TotalSeats = eventItem.TotalSeats,
                    AvailableSeats = eventItem.AvailableSeats
                })
                .ToList();

            return new PaginatedResult<EventInfoDto>
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

            var newEvent = Event.Create(
                dto.Title,
                dto.Description,
                dto.StartAt,
                dto.EndAt,
                dto.TotalSeats
            );
            await _eventStore.AddAsync(newEvent);
            return newEvent.Id;
        }

        public async Task<EventInfoDto> UpdateEventAsync(Guid eventId, UpdateEventDto dto)
        {
            ValidateDates(dto.StartAt, dto.EndAt);

            var existingEvent = await GetEventAsync(eventId);

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartAt = dto.StartAt;
            existingEvent.EndAt = dto.EndAt;
            existingEvent.TotalSeats = dto.TotalSeats;

            await _eventStore.UpdateAsync(existingEvent);
            return new EventInfoDto
            {
                Id = existingEvent.Id,
                Title = existingEvent.Title,
                Description = existingEvent.Description,
                StartAt = existingEvent.StartAt,
                EndAt = existingEvent.EndAt,
                TotalSeats = existingEvent.TotalSeats,
                AvailableSeats = existingEvent.AvailableSeats
            };
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