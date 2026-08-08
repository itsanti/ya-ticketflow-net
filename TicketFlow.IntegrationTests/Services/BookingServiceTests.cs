using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Services;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.IntegrationTests.Infrastructure;

namespace TicketFlow.IntegrationTests.Services
{
    [Collection("PostgreSql collection")]
    public class BookingServiceTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public BookingServiceTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<Event> StoreEvent(int totalSeats)
        {
            await using var context = _fixture.CreateContext();

            var eventItem = Event.Create(
                "Tech Conference",
                "Description",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                totalSeats);

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            return eventItem;
        }

        private async Task<User> StoreUser()
        {
            await using var context = _fixture.CreateContext();

            var user = User.Create($"user-{Guid.NewGuid()}", "hash", UserRole.User);

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldPersistBookingAndReserveSeat()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem = await StoreEvent(totalSeats: 10);
            var user = await StoreUser();

            await using var serviceProvider = _fixture.CreateServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                await bookingService.CreateBookingAsync(eventItem.Id, user.Id);
            }

            await using var context = _fixture.CreateContext();

            var storedBooking = await context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.EventId == eventItem.Id);

            var storedEvent = await context.Events
                .AsNoTracking()
                .FirstAsync(e => e.Id == eventItem.Id);

            Assert.NotNull(storedBooking);
            Assert.Equal(BookingStatus.Pending, storedBooking.Status);
            Assert.Equal(9, storedEvent.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldNotPersistBooking_WhenEventIsSoldOut()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem = await StoreEvent(totalSeats: 1);
            var firstUser = await StoreUser();
            var secondUser = await StoreUser();

            await using var serviceProvider = _fixture.CreateServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                await bookingService.CreateBookingAsync(eventItem.Id, firstUser.Id);
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                    bookingService.CreateBookingAsync(eventItem.Id, secondUser.Id));
            }

            await using var context = _fixture.CreateContext();

            var storedBookings = await context.Bookings
                .AsNoTracking()
                .Where(b => b.EventId == eventItem.Id)
                .ToListAsync();

            var storedEvent = await context.Events
                .AsNoTracking()
                .FirstAsync(e => e.Id == eventItem.Id);

            Assert.Single(storedBookings);
            Assert.Equal(0, storedEvent.AvailableSeats);
        }
    }
}
