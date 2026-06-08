using TicketFlow.DTOs.Events;
using TicketFlow.Exceptions;
using TicketFlow.Models.Store;
using TicketFlow.Services;

namespace TicketFlow.Tests
{
    public class EventServiceTests
    {
        private readonly List<CreateEventDto> _events = new()
        {
            new CreateEventDto
            {
                Title = "Концерт классической музыки",
                Description = "Вечер Бетховена в филармонии",
                StartAt = new DateTime(2026, 06, 01, 19, 0, 0),
                EndAt = new DateTime(2026, 06, 01, 21, 0, 0),
                TotalSeats = 1,
            },
            new CreateEventDto
            {
                Title = "IT-Конференция",
                Description = "Тренды разработки на .NET 10",
                StartAt = new DateTime(2026, 07, 15, 10, 0, 0),
                EndAt = new DateTime(2026, 07, 16, 18, 0, 0),
                TotalSeats = 1,
            },
            new CreateEventDto
            {
                Title = "Выставка роботов",
                Description = "Короткое интерактивное шоу",
                StartAt = new DateTime(2026, 08, 10, 12, 0, 0),
                EndAt = new DateTime(2026, 08, 10, 12, 30, 0),
                TotalSeats = 1,
            }
        };

        [Fact]
        public async Task AddEvent_ShouldReturnGuid_AndStoreEventInService()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var eventItem = _events.First();

            var result = await service.AddEventAsync(eventItem);
            var savedEvent = await service.GetEventAsync(result);

            Assert.NotEqual(Guid.Empty, result);
            Assert.NotNull(savedEvent);
            Assert.Equal(eventItem.Title, savedEvent.Title);

        }

        [Fact]
        public async Task GetEvents_ShouldReturnAllStoredEvents_WhenNoFiltersApplied()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events)
            {
                await service.AddEventAsync(dto);
            }
            var filters = new EventFiltersDto();

            var result = await service.GetEventsAsync(filters);

            Assert.Equal(_events.Count, result.TotalCount);
            Assert.Equal(_events.Count, result.Items.Count());
        }


        [Fact]
        public async Task GetEvent_ShouldReturnCorrectEvent_WhenIdExists()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var _event = _events.First();
            var id = await service.AddEventAsync(_event);

            var result = await service.GetEventAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal(_event.Title, result.Title);
        }

        [Fact]
        public async Task UpdateEvent_ShouldModifyStoredEvent_WhenIdExists()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var _event = _events.First();
            var id = await service.AddEventAsync(_event);
            var updateDto = new UpdateEventDto
            {
                Title = "Новый Концерт",
                Description = "Вечер Бетховена в филармонии",
                StartAt = new DateTime(2026, 06, 02, 19, 0, 0),
                EndAt = new DateTime(2026, 06, 02, 21, 0, 0),
                TotalSeats = 1,
            };

            await service.UpdateEventAsync(id, updateDto);
            var result = await service.GetEventAsync(id);

            Assert.Equal(updateDto.Title, result.Title);
            Assert.Equal(updateDto.StartAt, result.StartAt);
            Assert.Equal(updateDto.EndAt, result.EndAt);

        }

        [Fact]
        public async Task RemoveEvent_ShouldReturnTrue_AndRemoveEventFromService()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var id = await service.AddEventAsync(_events.First());

            var deleted = await service.RemoveEventAsync(id);

            Assert.True(deleted);
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetEventAsync(id));
        }

        [Fact]
        public async Task GetEvents_ShouldFilterByTitle_IgnoringCase()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events)
            {
                await service.AddEventAsync(dto);
            }

            var filters = new EventFiltersDto { Title = "КОНЦЕРТ" };

            var result = await service.GetEventsAsync(filters);

            Assert.Single(result.Items);
            Assert.Contains("Концерт", result.Items.First().Title);

        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetEvents_ShouldIgnoreTitleFilter_WhenTitleIsNullOrWhiteSpace(string? emptyTitle)
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events) await service.AddEventAsync(dto);

            var filters = new EventFiltersDto { Title = emptyTitle };

            var result = await service.GetEventsAsync(filters);

            Assert.Equal(_events.Count, result.TotalCount);
        }

        [Fact]
        public async Task GetEvents_ShouldFilterByDateRange()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events)
            {
                await service.AddEventAsync(dto);
            }
            var filters = new EventFiltersDto
            {
                From = new DateTime(2026, 07, 10, 10, 0, 0),
                To = new DateTime(2026, 07, 20, 10, 0, 0)
            };

            var result = await service.GetEventsAsync(filters);

            Assert.Single(result.Items);
            Assert.Equal("IT-Конференция", result.Items.First().Title);
        }

        [Fact]
        public async Task GetEvents_ShouldIncludeEvent_WhenItStartsExactlyAtFromDate()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var targetDate = new DateTime(2026, 01, 01, 10, 0, 0);
            var dto = new CreateEventDto
            {
                Title = "Border Event",
                StartAt = targetDate,
                EndAt = targetDate.AddHours(1),
                TotalSeats = 1,
            };
            await service.AddEventAsync(dto);

            var filters = new EventFiltersDto { From = targetDate };
            var result = await service.GetEventsAsync(filters);

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetEvents_ShouldReturnCorrectPage_WithPagination()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events.Concat(_events))
            {
                await service.AddEventAsync(dto);
            }
            var filters = new EventFiltersDto
            {
                Page = 2,
                PageSize = 2
            };

            var result = await service.GetEventsAsync(filters);

            Assert.Equal(2, result.Items.Count());
            Assert.Equal(2 * _events.Count, result.TotalCount);
            Assert.Equal(2, result.Page);
        }

        [Fact]
        public async Task GetEvents_ShouldReturnActualItemCountInPageSize()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events)
            {
                await service.AddEventAsync(dto);
            }

            var filters = new EventFiltersDto
            {
                Page = 2,
                PageSize = 2
            };

            var result = await service.GetEventsAsync(filters);

            Assert.Single(result.Items);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.PageSize);
        }

        [Fact]
        public async Task GetEvents_ShouldFilterByTitleAndDateCombined()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            foreach (var dto in _events)
            {
                await service.AddEventAsync(dto);
            }
            var filters = new EventFiltersDto
            {
                Title = "Концерт",
                From = new DateTime(2026, 8, 1)
            };

            var result = await service.GetEventsAsync(filters);

            Assert.Empty(result.Items);

        }

        [Fact]
        public async Task GetEvent_ShouldThrowNotFoundException()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetEventAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateEvent_ShouldThrowNotFoundException_WhenIdDoesNotExist()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var updateDto = new UpdateEventDto { Title = "New", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateEventAsync(Guid.NewGuid(), updateDto));
        }

        [Fact]
        public async Task AddEvent_ShouldThrowValidationException_WhenDatesAreInvalid()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var invalidDto = new CreateEventDto
            {
                Title = "Fail",
                StartAt = new DateTime(2026, 10, 10),
                EndAt = new DateTime(2026, 10, 09),
                TotalSeats = 1,
            };

            await Assert.ThrowsAsync<ValidationException>(() => service.AddEventAsync(invalidDto));
        }

        [Fact]
        public async Task UpdateEvent_ShouldThrowValidationException_WhenNewDatesAreInvalid()
        {
            var store = new InMemoryEventStore();
            var service = new EventService(store);

            var id = await service.AddEventAsync(_events.First());
            var invalidUpdate = new UpdateEventDto
            {
                Title = "Valid Title",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now,
                TotalSeats = 1,
            };

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateEventAsync(id, invalidUpdate));
        }
    }
}
