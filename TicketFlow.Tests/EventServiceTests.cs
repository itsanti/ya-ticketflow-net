using TicketFlow.DTOs.Events;
using TicketFlow.Exceptions;
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
            },
            new CreateEventDto
            {
                Title = "IT-Конференция",
                Description = "Тренды разработки на .NET 10",
                StartAt = new DateTime(2026, 07, 15, 10, 0, 0),
                EndAt = new DateTime(2026, 07, 16, 18, 0, 0),
            },
            new CreateEventDto
            {
                Title = "Выставка роботов",
                Description = "Короткое интерактивное шоу",
                StartAt = new DateTime(2026, 08, 10, 12, 0, 0),
                EndAt = new DateTime(2026, 08, 10, 12, 30, 0),
            }
        };

        [Fact]
        public void AddEvent_ShouldReturnGuid_AndStoreEventInService()
        {
            var service = new EventService();

            var eventItem = _events.First();

            var result = service.AddEvent(eventItem);
            var savedEvent = service.GetEvent(result);

            Assert.NotEqual(Guid.Empty, result);
            Assert.NotNull(savedEvent);
            Assert.Equal(eventItem.Title, savedEvent.Title);

        }

        [Fact]
        public void GetEvents_ShouldReturnAllStoredEvents_WhenNoFiltersApplied()
        {
            var service = new EventService();
            foreach (var dto in _events)
            {
                service.AddEvent(dto);
            }
            var filters = new EventFiltersDto();

            var result = service.GetEvents(filters);

            Assert.Equal(_events.Count, result.TotalCount);
            Assert.Equal(_events.Count, result.Items.Count());
        }


        [Fact]
        public void GetEvent_ShouldReturnCorrectEvent_WhenIdExists()
        {
            var service = new EventService();
            var _event = _events.First();
            var id = service.AddEvent(_event);

            var result = service.GetEvent(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal(_event.Title, result.Title);
        }

        [Fact]
        public void UpdateEvent_ShouldModifyStoredEvent_WhenIdExists()
        {
            var service = new EventService();
            var _event = _events.First();
            var id = service.AddEvent(_event);
            var updateDto = new UpdateEventDto
            {
                Title = "Новый Концерт",
                Description = "Вечер Бетховена в филармонии",
                StartAt = new DateTime(2026, 06, 02, 19, 0, 0),
                EndAt = new DateTime(2026, 06, 02, 21, 0, 0),
            };

            service.UpdateEvent(id, updateDto);
            var result = service.GetEvent(id);

            Assert.Equal(updateDto.Title, result.Title);
            Assert.Equal(updateDto.StartAt, result.StartAt);
            Assert.Equal(updateDto.EndAt, result.EndAt);

        }

        [Fact]
        public void RemoveEvent_ShouldReturnTrue_AndRemoveEventFromService()
        {
            var service = new EventService();
            var id = service.AddEvent(_events.First());

            var deleted = service.RemoveEvent(id);

            Assert.True(deleted);
            Assert.Throws<NotFoundException>(() => service.GetEvent(id));
        }

        [Fact]
        public void GetEvents_ShouldFilterByTitle_IgnoringCase()
        {
            var service = new EventService();
            foreach (var dto in _events)
            {
                service.AddEvent(dto);
            }

            var filters = new EventFiltersDto { Title = "КОНЦЕРТ" };

            var result = service.GetEvents(filters);

            Assert.Single(result.Items);
            Assert.Contains("Концерт", result.Items.First().Title);

        }

        [Fact]
        public void GetEvents_ShouldFilterByDateRange()
        {
            var service = new EventService();
            foreach (var dto in _events)
            {
                service.AddEvent(dto);
            }
            var filters = new EventFiltersDto
            {
                From = new DateTime(2026, 07, 10, 10, 0, 0),
                To = new DateTime(2026, 07, 20, 10, 0, 0)
            };

            var result = service.GetEvents(filters);

            Assert.Single(result.Items);
            Assert.Equal("IT-Конференция", result.Items.First().Title);
        }

        [Fact]
        public void GetEvents_ShouldReturnCorrectPage_WithPagination()
        {
            var service = new EventService();
            foreach (var dto in _events.Concat(_events))
            {
                service.AddEvent(dto);
            }
            var filters = new EventFiltersDto
            {
                Page = 2,
                PageSize = 2
            };

            var result = service.GetEvents(filters);

            Assert.Equal(2, result.Items.Count());
            Assert.Equal(2 * _events.Count, result.TotalCount);
            Assert.Equal(2, result.Page);
        }

        [Fact]
        public void GetEvents_ShouldReturnActualItemCountInPageSize()
        {
            var service = new EventService();
            foreach (var dto in _events)
            {
                service.AddEvent(dto);
            }

            var filters = new EventFiltersDto
            {
                Page = 2,
                PageSize = 2
            };

            var result = service.GetEvents(filters);

            Assert.Single(result.Items);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.PageSize);
        }

        [Fact]
        public void GetEvents_ShouldFilterByTitleAndDateCombined()
        {
            var service = new EventService();
            foreach (var dto in _events)
            {
                service.AddEvent(dto);
            }
            var filters = new EventFiltersDto
            {
                Title = "Концерт",
                From = new DateTime(2026, 8, 1)
            };

            var result = service.GetEvents(filters);

            Assert.Empty(result.Items);

        }

        [Fact]
        public void GetEvent_ShouldThrowNotFoundException()
        {
            var service = new EventService();
            Assert.Throws<NotFoundException>(() => service.GetEvent(Guid.NewGuid()));
        }

        [Fact]
        public void UpdateEvent_ShouldThrowNotFoundException_WhenIdDoesNotExist()
        {
            var service = new EventService();
            var updateDto = new UpdateEventDto { Title = "New", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) };

            Assert.Throws<NotFoundException>(() => service.UpdateEvent(Guid.NewGuid(), updateDto));
        }

        [Fact]
        public void AddEvent_ShouldThrowValidationException_WhenDatesAreInvalid()
        {
            var service = new EventService();
            var invalidDto = new CreateEventDto
            {
                Title = "Fail",
                StartAt = new DateTime(2026, 10, 10),
                EndAt = new DateTime(2026, 10, 09)
            };

            Assert.Throws<ValidationException>(() => service.AddEvent(invalidDto));
        }

        [Fact]
        public void UpdateEvent_ShouldThrowValidationException_WhenNewDatesAreInvalid()
        {
            var service = new EventService();
            var id = service.AddEvent(_events.First());
            var invalidUpdate = new UpdateEventDto
            {
                Title = "Valid Title",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now
            };

            Assert.Throws<ValidationException>(() => service.UpdateEvent(id, invalidUpdate));
        }
    }
}
