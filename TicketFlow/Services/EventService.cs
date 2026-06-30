using TicketFlow.DataAccess.Repositories;
using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Exceptions;
using TicketFlow.Models;

namespace TicketFlow.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;

        public EventService(IEventRepository eventRepo)
        {
            _eventRepo = eventRepo;
        }

        public async Task<PaginatedResult<EventInfoDto>> GetEventsAsync(EventFiltersDto filters)
        {

            var (items, totalCount) = await _eventRepo.GetPagedAsync(filters);

            return new PaginatedResult<EventInfoDto>
            {
                Items = items.Select(eventItem => new EventInfoDto
                {
                    Id = eventItem.Id,
                    Title = eventItem.Title,
                    Description = eventItem.Description,
                    StartAt = eventItem.StartAt,
                    EndAt = eventItem.EndAt,
                    TotalSeats = eventItem.TotalSeats,
                    AvailableSeats = eventItem.AvailableSeats
                }),
                TotalCount = totalCount,
                Page = filters.Page,
                PageSize = items.Count
            };
        }

        private async Task<Event> GetEventEntityAsync(Guid eventId)
        {
            Event? eventItem = await _eventRepo.GetByIdAsync(eventId);

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

            await _eventRepo.AddAsync(newEvent);
            await _eventRepo.SaveChangesAsync();

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

            await _eventRepo.SaveChangesAsync();

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
            _eventRepo.Remove(eventItem);
            await _eventRepo.SaveChangesAsync();
            return true;
        }

        private static void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (endAt <= startAt)
                throw new ValidationException("EndAt must be greater than StartAt");
        }
    }
}