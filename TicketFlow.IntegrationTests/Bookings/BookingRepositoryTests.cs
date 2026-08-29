using Microsoft.EntityFrameworkCore;
using TicketFlow.Bookings.Domain.Entities;
using TicketFlow.Bookings.Domain.Enums;
using TicketFlow.Bookings.Infrastructure.Repositories;

namespace TicketFlow.IntegrationTests.Bookings
{
    // Тест на DbUpdateException при несуществующем событии удалён без замены: FK
    // bookings.event_id -> events.id убрали вместе с общей БД — вставка с любым
    // eventId теперь ожидаемо проходит (согласованность в конечном счёте через Kafka).
    [Collection("Bookings PostgreSql collection")]
    public class BookingRepositoryTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public BookingRepositoryTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddAsync_ShouldPersistBooking()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var booking = new Booking(eventId, userId);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();

            var storedBooking = await context.Bookings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(storedBooking);
            Assert.Equal(booking.Id, storedBooking.Id);
            Assert.Equal(eventId, storedBooking.EventId);
            Assert.Equal(userId, storedBooking.UserId);
            Assert.Equal(BookingStatus.Pending, storedBooking.Status);
            Assert.Null(storedBooking.ProcessedAt);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBooking_WhenBookingExists()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var eventId = Guid.NewGuid();
            var booking = new Booking(eventId, Guid.NewGuid());

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var storedBooking = await repository.GetByIdAsync(booking.Id);

            Assert.NotNull(storedBooking);
            Assert.Equal(booking.Id, storedBooking.Id);
            Assert.Equal(eventId, storedBooking.EventId);
            Assert.Equal(BookingStatus.Pending, storedBooking.Status);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var storedBooking = await repository.GetByIdAsync(Guid.Empty);

            Assert.Null(storedBooking);
        }

        [Fact]
        public async Task GetPendingIdsAsync_ShouldReturnOnlyPendingBookingIds()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var userId = Guid.NewGuid();

            var bookingPending = new Booking(Guid.NewGuid(), userId);
            var bookingConfirmed = new Booking(Guid.NewGuid(), userId);
            var bookingRejected = new Booking(Guid.NewGuid(), userId);

            bookingConfirmed.Confirm();
            bookingRejected.Reject();

            await context.Bookings.AddRangeAsync(
                bookingPending,
                bookingConfirmed,
                bookingRejected);

            await context.SaveChangesAsync();

            var pendings = await repository.GetPendingIdsAsync();

            Assert.Contains(bookingPending.Id, pendings);
            Assert.DoesNotContain(bookingConfirmed.Id, pendings);
            Assert.DoesNotContain(bookingRejected.Id, pendings);
            Assert.Single(pendings);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldPersistConfirmedStatus()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var storedBooking = await repository.GetByIdAsync(booking.Id);
            storedBooking!.Confirm();
            await repository.SaveChangesAsync();

            storedBooking = await context.Bookings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == booking.Id);
            Assert.Equal(BookingStatus.Confirmed, storedBooking!.Status);
            Assert.NotNull(storedBooking.ProcessedAt);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldPersistRejectedStatus()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var storedBooking = await repository.GetByIdAsync(booking.Id);
            storedBooking!.Reject();
            await repository.SaveChangesAsync();

            storedBooking = await context.Bookings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == booking.Id);
            Assert.Equal(BookingStatus.Rejected, storedBooking!.Status);
            Assert.NotNull(storedBooking.ProcessedAt);
        }

        [Fact]
        public async Task GetByIdAsNoTrackingAsync_ShouldReturnBookingWithoutTracking_WhenBookingExists()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var storedBooking = await repository.GetByIdAsNoTrackingAsync(booking.Id);

            storedBooking!.Confirm();
            await repository.SaveChangesAsync();

            var bookingFromDatabase = await context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(storedBooking);
            Assert.NotNull(bookingFromDatabase);
            Assert.Equal(BookingStatus.Pending, bookingFromDatabase.Status);
            Assert.Null(bookingFromDatabase.ProcessedAt);
        }
    }
}
