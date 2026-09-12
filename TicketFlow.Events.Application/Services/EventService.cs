using Microsoft.Extensions.Options;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.Caching;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Application.DTOs.Pagination;
using TicketFlow.Events.Application.Options;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly ICacheService _cache;
        private readonly CacheOptions _cacheOptions;

        public EventService(IEventRepository eventRepo, ICacheService cache, IOptions<CacheOptions> cacheOptions)
        {
            _eventRepo = eventRepo;
            _cache = cache;
            _cacheOptions = cacheOptions.Value;
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
                PageSize = filters.PageSize
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
            var key = CacheKeys.EventKey(eventId);
            var eventItem = await _cache.GetAsync<EventInfoDto>(key);

            if (eventItem == null)
            {
                Event eventEntity = await GetEventEntityAsync(eventId);
                eventItem = new EventInfoDto
                {
                    Id = eventEntity.Id,
                    Title = eventEntity.Title,
                    Description = eventEntity.Description,
                    StartAt = eventEntity.StartAt,
                    EndAt = eventEntity.EndAt,
                    TotalSeats = eventEntity.TotalSeats,
                    AvailableSeats = eventEntity.AvailableSeats
                };

                await _cache.SetAsync(key, eventItem, TimeSpan.FromSeconds(_cacheOptions.EventTtlSeconds));
            }

            return eventItem;
        }

        public async Task<IReadOnlyList<EventInfoDto>> GetTopEventsAsync()
        {
            var key = CacheKeys.TopEventsKey;
            var events = await _cache.GetAsync<IReadOnlyList<EventInfoDto>>(key);

            if (events == null)
            {
                var topEvents = await _eventRepo.GetTopPopularAsync(10);
                events = topEvents.Select(eventItem => new EventInfoDto
                {
                    Id = eventItem.Id,
                    Title = eventItem.Title,
                    Description = eventItem.Description,
                    StartAt = eventItem.StartAt,
                    EndAt = eventItem.EndAt,
                    TotalSeats = eventItem.TotalSeats,
                    AvailableSeats = eventItem.AvailableSeats
                }).ToList();

                await _cache.SetAsync(key, events, TimeSpan.FromSeconds(_cacheOptions.TopEventsTtlSeconds));
            }
            return events;
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
            ValidateTotalSeats(dto.TotalSeats);

            var existingEvent = await GetEventEntityAsync(eventId);

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartAt = dto.StartAt;
            existingEvent.EndAt = dto.EndAt;
            existingEvent.TotalSeats = dto.TotalSeats;

            await _eventRepo.SaveChangesAsync();

            // Delete-on-Write: следующее чтение прогреет кеш заново.
            await _cache.RemoveAsync(CacheKeys.EventKey(eventId));

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

        public async Task RemoveEventAsync(Guid eventId)
        {
            var eventItem = await GetEventEntityAsync(eventId);
            _eventRepo.Remove(eventItem);
            await _eventRepo.SaveChangesAsync();

            await _cache.RemoveAsync(CacheKeys.EventKey(eventId));
        }

        private static void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (endAt <= startAt)
                throw new ValidationException("EndAt must be greater than StartAt");
        }

        private static void ValidateTotalSeats(int totalSeats)
        {
            if (totalSeats <= 0)
                throw new ValidationException("TotalSeats must be greater than 0");
        }
    }
}
