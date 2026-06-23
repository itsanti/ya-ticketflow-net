using Microsoft.EntityFrameworkCore;
using TicketFlow.DataAccess;
using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Exceptions;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;

        public EventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<EventInfoDto>> GetEventsAsync(EventFiltersDto filters)
        {
            IQueryable<Event> query = _context.Events.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filters.Title))
            {
                var title = filters.Title.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(title));
            }

            if (filters.From.HasValue)
            {
                query = query.Where(e => e.StartAt >= filters.From.Value);
            }

            if (filters.To.HasValue)
            {
                query = query.Where(e => e.EndAt <= filters.To.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
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
                .ToListAsync();

            return new PaginatedResult<EventInfoDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filters.Page,
                PageSize = items.Count
            };
        }

        private async Task<Event> GetEventEntityAsync(Guid eventId)
        {
            Event? eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventItem == null)
            {
                throw new NotFoundException($"Event with ID {eventId} not found.");
            }

            return eventItem;
        }

        public async Task<EventInfoDto> GetEventAsync(Guid eventId)
        {
            var eventItem = await GetEventEntityAsync(eventId);
            return new EventInfoDto
            {
                Id = eventItem.Id,
                Title = eventItem.Title,
                Description = eventItem.Description,
                StartAt = eventItem.StartAt,
                EndAt = eventItem.EndAt,
                TotalSeats = eventItem.TotalSeats,
                AvailableSeats = eventItem.AvailableSeats
            };
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
            await _context.Events.AddAsync(newEvent);
            await _context.SaveChangesAsync();
            return newEvent.Id;
        }

        public async Task<EventInfoDto> UpdateEventAsync(Guid eventId, UpdateEventDto dto)
        {
            ValidateDates(dto.StartAt, dto.EndAt);

            var existingEvent = await GetEventEntityAsync(eventId);

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartAt = dto.StartAt;
            existingEvent.EndAt = dto.EndAt;
            existingEvent.TotalSeats = dto.TotalSeats;

            await _context.SaveChangesAsync();

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
            var eventItem = await GetEventEntityAsync(eventId);
            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (endAt <= startAt)
                throw new ValidationException("EndAt must be greater than StartAt");
        }
    }
}