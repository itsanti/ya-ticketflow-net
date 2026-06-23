using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.DataAccess;
using TicketFlow.Exceptions;
using TicketFlow.Models;
using TicketFlow.Services;

namespace TicketFlow.Tests
{
    public class BookingServiceTests
    {
        [Fact]
        public async Task CreateBooking_ShouldReturnPendingBooking_WhenEventExists()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var eventItem = TestHelpers.CreateTestEvent(2);
            var eventId = Guid.NewGuid();
            eventItem.Id = eventId;

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var booking = await bookingService.CreateBookingAsync(eventItem.Id);

            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.True(booking.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();
            var eventItem = TestHelpers.CreateTestEvent(2);
            eventItem.Id = eventId;
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var booking1 = await bookingService.CreateBookingAsync(eventId);
            var booking2 = await bookingService.CreateBookingAsync(eventId);

            Assert.NotEqual(booking1.Id, booking2.Id);
        }

        [Fact]
        public async Task GetBookingById_ShouldReturnCorrectBooking_WhenIdExists()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();
            var eventItem = TestHelpers.CreateTestEvent(2);
            eventItem.Id = eventId;
            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var createdBooking = await bookingService.CreateBookingAsync(eventId);

            var retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

            Assert.NotNull(retrievedBooking);
            Assert.Equal(createdBooking.Id, retrievedBooking.Id);
            Assert.Equal(eventId, retrievedBooking.EventId);
            Assert.Equal(BookingStatus.Pending, retrievedBooking.Status);
        }

        [Fact]
        public async Task GetBookingById_ShouldThrowNotFoundException_WhenIdDoesNotExist()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                bookingService.GetBookingByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetBooking_ShouldReflectStatusChange_AfterDatabaseUpdate()
        {
            using var serviceProvider = TestHelpers.Create();

            var eventItem = TestHelpers.CreateTestEvent(2);

            using (var setupScope = serviceProvider.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Events.AddAsync(eventItem);
                await context.SaveChangesAsync();
            }

            Booking booking;

            using (var createScope = serviceProvider.CreateScope())
            {
                var bookingService = createScope.ServiceProvider.GetRequiredService<IBookingService>();

                booking = await bookingService.CreateBookingAsync(eventItem.Id);
            }

            using (var updateScope = serviceProvider.CreateScope())
            {
                var context = updateScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var bookingToUpdate = await context.Bookings
                    .FirstAsync(b => b.Id == booking.Id);

                bookingToUpdate.Status = BookingStatus.Confirmed;
                bookingToUpdate.ProcessedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }

            using (var verificationScope = serviceProvider.CreateScope())
            {
                var bookingService = verificationScope.ServiceProvider.GetRequiredService<IBookingService>();

                var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id);

                Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
                Assert.NotNull(updatedBooking.ProcessedAt);
            }
        }

        [Fact]
        public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventDoesNotExist()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var fakeEventId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                bookingService.CreateBookingAsync(fakeEventId));
        }

        [Fact]
        public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventWasDeleted()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            Event eventItem = TestHelpers.CreateTestEvent(1);

            await context.Events.AddAsync(eventItem);
            context.Events.Remove(eventItem);
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                bookingService.CreateBookingAsync(eventItem.Id));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats_WhenBookingIsSuccessful()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = TestHelpers.CreateTestEvent(10);

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            var booking = await bookingService.CreateBookingAsync(eventItem.Id);

            using (var verificationScope = serviceProvider.CreateScope())
            {
                var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var storedEvent = await verificationContext.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == eventItem.Id);

                Assert.NotNull(storedEvent);
                Assert.Equal(9, storedEvent.AvailableSeats);
            }
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenEventIsSoldOut()
        {
            using var serviceProvider = TestHelpers.Create();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = TestHelpers.CreateTestEvent(1);

            await context.Events.AddAsync(eventItem);
            await context.SaveChangesAsync();

            await bookingService.CreateBookingAsync(eventItem.Id);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                bookingService.CreateBookingAsync(eventItem.Id));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldPreventOverbooking_UnderConcurrentLoad()
        {
            using var serviceProvider = TestHelpers.Create();

            var eventItem = TestHelpers.CreateTestEvent(5);

            using (var setupScope = serviceProvider.CreateScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Events.AddAsync(eventItem);
                await context.SaveChangesAsync();
            }

            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(async () =>
                {
                    using var requestScope = serviceProvider.CreateScope();

                    var bookingService = requestScope.ServiceProvider.GetRequiredService<IBookingService>();

                    try
                    {
                        var booking = await bookingService.CreateBookingAsync(eventItem.Id);
                        return booking.Id;
                    }
                    catch (NoAvailableSeatsException)
                    {
                        return Guid.Empty;
                    }
                }));

            var results = await Task.WhenAll(tasks);

            var successfulBookingIds = results
                .Where(id => id != Guid.Empty)
                .ToList();

            using (var verificationScope = serviceProvider.CreateScope())
            {
                var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var allBookings = await context.Bookings
                    .AsNoTracking()
                    .ToListAsync();

                var updatedEvent = await context.Events
                    .AsNoTracking()
                    .FirstAsync(e => e.Id == eventItem.Id);

                Assert.Equal(5, successfulBookingIds.Count);
                Assert.Equal(5, successfulBookingIds.Distinct().Count());
                Assert.Equal(5, allBookings.Count);
                Assert.Equal(0, updatedEvent.AvailableSeats);
            }
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldGenerateUniqueIds_UnderConcurrentLoad()
        {
            using var serviceProvider = TestHelpers.Create();

            int seats = 10;

            var eventItem = TestHelpers.CreateTestEvent(seats);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Events.AddAsync(eventItem);
                await context.SaveChangesAsync();
            }

            var tasks = Enumerable.Range(0, seats)
                    .Select(_ => Task.Run(async () =>
                    {
                        using var scope = serviceProvider.CreateScope();

                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                        return await bookingService.CreateBookingAsync(eventItem.Id);
                    }));

            var bookings = await Task.WhenAll(tasks);

            Assert.Equal(seats, bookings.Length);
            Assert.Equal(seats, bookings.Select(b => b.Id).Distinct().Count());

            using (var verificationScope = serviceProvider.CreateScope())
            {
                var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var allBookings = await context.Bookings.ToListAsync();

                Assert.Equal(seats, allBookings.Count);
                Assert.Equal(seats, allBookings.Select(b => b.Id).Distinct().Count());
            }
        }
    }
}
