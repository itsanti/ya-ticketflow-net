using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketFlow.Events.Application.Caching;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Application.Services;

namespace TicketFlow.Tests.Events
{
    public class EventCachingTests
    {
        private static readonly CreateEventDto SampleEvent = new()
        {
            Title = "Кешируемое событие",
            Description = "Для тестов кеша",
            StartAt = new DateTime(2026, 09, 01, 19, 0, 0),
            EndAt = new DateTime(2026, 09, 01, 21, 0, 0),
            TotalSeats = 5,
        };

        [Fact]
        public async Task GetEventAsync_ShouldReturnCachedValue_AndNotCallRepository_OnCacheHit()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var eventId = Guid.NewGuid();
            var cached = new EventInfoDto
            {
                Id = eventId,
                Title = "Из кеша",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 10,
                AvailableSeats = 10
            };

            env.CacheService
                .Setup(c => c.GetAsync<EventInfoDto>(CacheKeys.EventKey(eventId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cached);

            var result = await service.GetEventAsync(eventId);

            Assert.Equal(cached.Title, result.Title);
            env.EventRepository.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetEventAsync_ShouldReadFromRepository_AndPopulateCache_OnCacheMiss()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var id = await service.AddEventAsync(SampleEvent);

            env.CacheService
                .Setup(c => c.GetAsync<EventInfoDto>(CacheKeys.EventKey(id), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EventInfoDto?)null);

            var result = await service.GetEventAsync(id);

            Assert.Equal(SampleEvent.Title, result.Title);
            env.EventRepository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            env.CacheService.Verify(
                c => c.SetAsync(
                    CacheKeys.EventKey(id),
                    It.Is<EventInfoDto>(dto => dto.Id == id),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTopEventsAsync_ShouldReturnCachedValue_AndNotCallRepository_OnCacheHit()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            IReadOnlyList<EventInfoDto> cached =
            [
                new EventInfoDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Топ-1",
                    StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(1),
                    TotalSeats = 10,
                    AvailableSeats = 0
                }
            ];

            env.CacheService
                .Setup(c => c.GetAsync<IReadOnlyList<EventInfoDto>>(CacheKeys.TopEventsKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cached);

            var result = await service.GetTopEventsAsync();

            Assert.Single(result);
            Assert.Equal(cached[0].Title, result[0].Title);
            env.EventRepository.Verify(
                r => r.GetTopPopularAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetTopEventsAsync_ShouldReadFromRepository_AndPopulateCache_OnCacheMiss()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            await service.AddEventAsync(SampleEvent);

            env.CacheService
                .Setup(c => c.GetAsync<IReadOnlyList<EventInfoDto>>(CacheKeys.TopEventsKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<EventInfoDto>?)null);

            var result = await service.GetTopEventsAsync();

            Assert.Single(result);
            env.EventRepository.Verify(
                r => r.GetTopPopularAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            env.CacheService.Verify(
                c => c.SetAsync(
                    CacheKeys.TopEventsKey,
                    It.IsAny<IReadOnlyList<EventInfoDto>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_ShouldInvalidateEventCache_AfterSaving()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var id = await service.AddEventAsync(SampleEvent);
            var updateDto = new UpdateEventDto
            {
                Title = "Обновлено",
                StartAt = SampleEvent.StartAt,
                EndAt = SampleEvent.EndAt,
                TotalSeats = SampleEvent.TotalSeats,
            };

            await service.UpdateEventAsync(id, updateDto);

            env.CacheService.Verify(
                c => c.RemoveAsync(CacheKeys.EventKey(id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveEventAsync_ShouldInvalidateEventCache_AfterDeleting()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var id = await service.AddEventAsync(SampleEvent);

            await service.RemoveEventAsync(id);

            env.CacheService.Verify(
                c => c.RemoveAsync(CacheKeys.EventKey(id), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
