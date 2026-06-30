using Microsoft.EntityFrameworkCore;
using TicketFlow.DataAccess;
using TicketFlow.DataAccess.Repositories;
using TicketFlow.IntegrationTests.Infrastructure;
using TicketFlow.Models;

namespace TicketFlow.IntegrationTests.Repositories
{
    [Collection("PostgreSql collection")]
    public class BookingRepositoryTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public BookingRepositoryTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<Event> StoreEvent(AppDbContext context)
        {
            var eventItem = Event.Create(
                "Tech Conference",
                "Description",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                100);

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            return eventItem;
        }

        [Fact]
        public async Task AddAsync_ShouldPersistBooking()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var eventItem = await StoreEvent(context);

            var booking = new Booking(eventItem.Id);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();

            var storedBooking = await context.Bookings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(storedBooking);
            Assert.Equal(booking.Id, storedBooking.Id);
            Assert.Equal(eventItem.Id, storedBooking.EventId);
            Assert.Equal(BookingStatus.Pending, storedBooking.Status);
            Assert.Null(storedBooking.ProcessedAt);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBooking_WhenBookingExists()
        {
            await _fixture.ResetDatabaseAsync();
            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var eventItem = await StoreEvent(context);
            var booking = new Booking(eventItem.Id);

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var storedBooking = await repository.GetByIdAsync(booking.Id);

            Assert.NotNull(storedBooking);
            Assert.Equal(booking.Id, storedBooking.Id);
            Assert.Equal(eventItem.Id, storedBooking.EventId);
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

            var eventItemPending = await StoreEvent(context);
            var eventItemConfirmed = await StoreEvent(context);
            var eventItemRejected = await StoreEvent(context);

            var bookingPending = new Booking(eventItemPending.Id);
            var bookingConfirmed = new Booking(eventItemConfirmed.Id);
            var bookingRejected = new Booking(eventItemRejected.Id);

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

            var eventItem = await StoreEvent(context);
            var booking = new Booking(eventItem.Id);

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

            var eventItem = await StoreEvent(context);
            var booking = new Booking(eventItem.Id);

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
        public async Task AddAsync_ShouldThrowDbUpdateException_WhenEventDoesNotExist()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var booking = new Booking(Guid.NewGuid());

            await repository.AddAsync(booking);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                repository.SaveChangesAsync());
        }

        [Fact]
        public async Task GetByIdAsNoTrackingAsync_ShouldReturnBookingWithoutTracking_WhenBookingExists()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new BookingRepository(context);

            var eventItem = await StoreEvent(context);
            var booking = new Booking(eventItem.Id);

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
