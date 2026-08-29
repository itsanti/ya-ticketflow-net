using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.DependencyInjection;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Tests.Events
{
    internal sealed class TestEnvironment : IDisposable
    {
        private readonly object _sync = new();
        private readonly List<Event> _events = [];

        public ServiceProvider Provider { get; }

        public Mock<IEventRepository> EventRepository { get; } = new();

        public TestEnvironment()
        {
            SetupEventRepository();

            var services = new ServiceCollection();

            services.AddSingleton(EventRepository.Object);
            services.AddApplicationServices();

            Provider = services.BuildServiceProvider();
        }

        public IServiceScope CreateScope() => Provider.CreateScope();

        public void Dispose() => Provider.Dispose();

        public void SeedEvent(Event eventItem)
        {
            lock (_sync)
            {
                _events.Add(eventItem);
            }
        }

        public void RemoveEvent(Event eventItem)
        {
            lock (_sync)
            {
                _events.Remove(eventItem);
            }
        }

        public Event? FindEvent(Guid id)
        {
            lock (_sync)
            {
                return _events.FirstOrDefault(e => e.Id == id);
            }
        }

        private void SetupEventRepository()
        {
            EventRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => FindEvent(id));

            EventRepository
                .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
                .Callback((Event eventItem, CancellationToken _) => SeedEvent(eventItem))
                .Returns(Task.CompletedTask);

            EventRepository
                .Setup(r => r.Remove(It.IsAny<Event>()))
                .Callback((Event eventItem) =>
                {
                    lock (_sync)
                    {
                        _events.Remove(eventItem);
                    }
                });

            EventRepository
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Повторяет семантику EventRepository.GetPagedAsync: фильтрация, сортировка, пагинация.
            EventRepository
                .Setup(r => r.GetPagedAsync(It.IsAny<EventFiltersDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EventFiltersDto filters, CancellationToken _) =>
                {
                    IEnumerable<Event> query;

                    lock (_sync)
                    {
                        query = _events.ToList();
                    }

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

                    var totalCount = query.Count();

                    var items = query
                        .OrderBy(e => e.StartAt)
                        .ThenBy(e => e.Id)
                        .Skip((filters.Page - 1) * filters.PageSize)
                        .Take(filters.PageSize)
                        .ToList();

                    return ((IReadOnlyList<Event>)items, totalCount);
                });
        }
    }

    internal static class TestHelpers
    {
        internal static Event CreateTestEvent(int totalSeats)
        {
            return Event.Create(
                "Тестовое событие",
                "Описание тестового события",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                totalSeats
            );
        }

        public static TestEnvironment Create() => new();
    }
}
