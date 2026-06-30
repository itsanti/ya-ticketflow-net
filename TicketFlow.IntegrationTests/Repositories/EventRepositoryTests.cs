using Microsoft.EntityFrameworkCore;
using TicketFlow.DataAccess;
using TicketFlow.DataAccess.Repositories;
using TicketFlow.DTOs.Events;
using TicketFlow.IntegrationTests.Infrastructure;
using TicketFlow.Models;

namespace TicketFlow.IntegrationTests.Repositories
{
    [Collection("PostgreSql collection")]
    public class EventRepositoryTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public EventRepositoryTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static Event CreateEvent(
            string title,
            DateTime startAt,
            DateTime? endAt = null,
            int totalSeats = 100)
        {
            return Event.Create(
                title,
                "Description",
                startAt,
                endAt ?? startAt.AddHours(2),
                totalSeats);
        }

        private static async Task StoreEvents(AppDbContext context, params Event[] events)
        {
            await context.Events.AddRangeAsync(events);
            await context.SaveChangesAsync();
        }

        private async Task<Event> StoreEvent(AppDbContext context)
        {
            var baseDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            var eventItem = CreateEvent("Tech Conference", baseDate.AddDays(1));

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            return eventItem;
        }

        [Fact]
        public async Task AddAsync_ShouldPersistEvent()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var eventItem = CreateEvent("Tech Conference",
                DateTime.UtcNow.AddDays(1));

            await repository.AddAsync(eventItem);
            await repository.SaveChangesAsync();

            var storedEvent = await context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventItem.Id);

            Assert.NotNull(storedEvent);
            Assert.Equal("Tech Conference", storedEvent.Title);
            Assert.Equal(100, storedEvent.TotalSeats);
            Assert.Equal(100, storedEvent.AvailableSeats);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEvent_WhenEventExists()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var eventItem = await StoreEvent(context);

            var storedEvent = await repository.GetByIdAsync(eventItem.Id);

            Assert.NotNull(storedEvent);
            Assert.Equal(eventItem.Id, storedEvent.Id);

        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenEventDoesNotExist()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var storedEvent = await repository.GetByIdAsync(Guid.Empty);

            Assert.Null(storedEvent);
        }

        [Fact]
        public async Task Remove_ShouldDeleteEvent()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var eventItem = await StoreEvent(context);

            repository.Remove(eventItem);
            await repository.SaveChangesAsync();

            var storedEvent = await repository.GetByIdAsync(eventItem.Id);

            Assert.Null(storedEvent);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldPersistUpdatedEvent()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var eventItem = await StoreEvent(context);

            var storedEvent = (await repository.GetByIdAsync(eventItem.Id))!;

            storedEvent.Title = "New Event Title";

            await repository.SaveChangesAsync();

            storedEvent = await context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventItem.Id);

            Assert.Equal("New Event Title", storedEvent!.Title);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldFilterByTitle_CaseInsensitivePartialMatch()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var baseDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            await StoreEvents(
                context,
                CreateEvent(".NET Conference", baseDate.AddDays(1)),
                CreateEvent("Java Meetup", baseDate.AddDays(2)),
                CreateEvent("Advanced DotNet Workshop", baseDate.AddDays(3))
            );

            var filters = new EventFiltersDto
            {
                Title = "net",
                Page = 1,
                PageSize = 10
            };

            var (items, totalCount) = await repository.GetPagedAsync(filters);

            Assert.Equal(2, totalCount);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, e => e.Title == ".NET Conference");
            Assert.Contains(items, e => e.Title == "Advanced DotNet Workshop");
            Assert.DoesNotContain(items, e => e.Title == "Java Meetup");
        }

        [Fact]
        public async Task GetPagedAsync_ShouldFilterByFromDate()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var baseDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            await StoreEvents(
                context,
                CreateEvent("Old Event", baseDate.AddDays(1)),
                CreateEvent("Target Event", baseDate.AddDays(5)),
                CreateEvent("Future Event", baseDate.AddDays(10))
            );

            var filters = new EventFiltersDto
            {
                From = baseDate.AddDays(5),
                Page = 1,
                PageSize = 10
            };

            var (items, totalCount) = await repository.GetPagedAsync(filters);

            Assert.Equal(2, totalCount);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, e => e.Title == "Target Event");
            Assert.Contains(items, e => e.Title == "Future Event");
            Assert.DoesNotContain(items, e => e.Title == "Old Event");
        }

        [Fact]
        public async Task GetPagedAsync_ShouldFilterByToDate()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var baseDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            await StoreEvents(
                context,
                CreateEvent("Early Event", baseDate.AddDays(1), baseDate.AddDays(1).AddHours(2)),
                CreateEvent("Target Event", baseDate.AddDays(5), baseDate.AddDays(5).AddHours(2)),
                CreateEvent("Late Event", baseDate.AddDays(10), baseDate.AddDays(10).AddHours(2))
            );

            var filters = new EventFiltersDto
            {
                To = baseDate.AddDays(5).AddHours(2),
                Page = 1,
                PageSize = 10
            };

            var (items, totalCount) = await repository.GetPagedAsync(filters);

            Assert.Equal(2, totalCount);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, e => e.Title == "Early Event");
            Assert.Contains(items, e => e.Title == "Target Event");
            Assert.DoesNotContain(items, e => e.Title == "Late Event");
        }

        [Fact]
        public async Task GetPagedAsync_ShouldApplyPagination_AndReturnTotalCountBeforePagination()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var baseDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            await StoreEvents(
                context,
                CreateEvent("Event 1", baseDate.AddDays(1)),
                CreateEvent("Event 2", baseDate.AddDays(2)),
                CreateEvent("Event 3", baseDate.AddDays(3))
            );

            var filters = new EventFiltersDto
            {
                Page = 2,
                PageSize = 1
            };

            var (items, totalCount) = await repository.GetPagedAsync(filters);

            Assert.Equal(3, totalCount);
            Assert.Single(items);
            Assert.Equal("Event 2", items[0].Title);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldApplyCombinedFilters()
        {

            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new EventRepository(context);

            var baseDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            await StoreEvents(
                context,
                CreateEvent(".NET Old", baseDate.AddDays(1), baseDate.AddDays(1).AddHours(2)),
                CreateEvent(".NET Target", baseDate.AddDays(5), baseDate.AddDays(5).AddHours(2)),
                CreateEvent(".NET Late", baseDate.AddDays(10), baseDate.AddDays(10).AddHours(2)),
                CreateEvent("Java Target", baseDate.AddDays(5), baseDate.AddDays(5).AddHours(2))
            );

            var filters = new EventFiltersDto
            {
                Title = "net",
                From = baseDate.AddDays(5),
                To = baseDate.AddDays(5).AddHours(2),
                Page = 1,
                PageSize = 10
            };

            var (items, totalCount) = await repository.GetPagedAsync(filters);

            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Equal(".NET Target", items[0].Title);
        }
    }
}
