using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Application.Services;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Tests
{
    public class BookingServiceTests
    {
        [Fact]
        public async Task CreateBooking_ShouldReturnPendingBooking_WhenEventExists()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();

            var eventItem = TestHelpers.CreateTestEvent(2);
            var eventId = Guid.NewGuid();
            eventItem.Id = eventId;
            var userId = Guid.NewGuid();

            env.SeedEvent(eventItem);

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var booking = await bookingService.CreateBookingAsync(eventItem.Id, userId);

            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(nameof(BookingStatus.Pending), booking.Status);
            Assert.True(booking.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();
            var eventItem = TestHelpers.CreateTestEvent(2);
            eventItem.Id = eventId;

            env.SeedEvent(eventItem);

            var booking1 = await bookingService.CreateBookingAsync(eventId, Guid.NewGuid());
            var booking2 = await bookingService.CreateBookingAsync(eventId, Guid.NewGuid());

            Assert.NotEqual(booking1.Id, booking2.Id);
        }

        [Fact]
        public async Task GetBookingById_ShouldReturnCorrectBooking_WhenIdExists()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();
            var eventItem = TestHelpers.CreateTestEvent(2);
            eventItem.Id = eventId;

            env.SeedEvent(eventItem);

            var createdBooking = await bookingService.CreateBookingAsync(eventId, Guid.NewGuid());

            var retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

            Assert.NotNull(retrievedBooking);
            Assert.Equal(createdBooking.Id, retrievedBooking.Id);
            Assert.Equal(eventId, retrievedBooking.EventId);
            Assert.Equal(nameof(BookingStatus.Pending), retrievedBooking.Status);
        }

        [Fact]
        public async Task GetBookingById_ShouldThrowNotFoundException_WhenIdDoesNotExist()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                bookingService.GetBookingByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetBooking_ShouldReflectStatusChange_AfterDatabaseUpdate()
        {
            using var env = TestHelpers.Create();

            var eventItem = TestHelpers.CreateTestEvent(2);

            env.SeedEvent(eventItem);

            BookingResponseDto booking;

            using (var createScope = env.CreateScope())
            {
                var bookingService = createScope.ServiceProvider.GetRequiredService<IBookingService>();

                booking = await bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid());
            }

            var bookingToUpdate = env.FindBooking(booking.Id);

            Assert.NotNull(bookingToUpdate);

            bookingToUpdate.Status = BookingStatus.Confirmed;
            bookingToUpdate.ProcessedAt = DateTime.UtcNow;

            using (var verificationScope = env.CreateScope())
            {
                var bookingService = verificationScope.ServiceProvider.GetRequiredService<IBookingService>();

                var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id);

                Assert.Equal(nameof(BookingStatus.Confirmed), updatedBooking.Status);
                Assert.NotNull(updatedBooking.ProcessedAt);
            }
        }

        [Fact]
        public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventDoesNotExist()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var fakeEventId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                bookingService.CreateBookingAsync(fakeEventId, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventWasDeleted()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            Event eventItem = TestHelpers.CreateTestEvent(1);

            env.SeedEvent(eventItem);
            env.RemoveEvent(eventItem);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats_WhenBookingIsSuccessful()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = TestHelpers.CreateTestEvent(10);

            env.SeedEvent(eventItem);

            await bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid());

            var storedEvent = env.FindEvent(eventItem.Id);

            Assert.NotNull(storedEvent);
            Assert.Equal(9, storedEvent.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldSaveChangesOnce_WhenBookingIsCreated()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = TestHelpers.CreateTestEvent(10);

            env.SeedEvent(eventItem);

            await bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid());

            env.BookingRepository.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            env.EventRepository.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenEventIsSoldOut()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventItem = TestHelpers.CreateTestEvent(1);

            env.SeedEvent(eventItem);

            await bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid());

            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldPreventOverbooking_UnderConcurrentLoad()
        {
            using var env = TestHelpers.Create();

            var eventItem = TestHelpers.CreateTestEvent(5);

            env.SeedEvent(eventItem);

            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(async () =>
                {
                    using var requestScope = env.CreateScope();

                    var bookingService = requestScope.ServiceProvider.GetRequiredService<IBookingService>();

                    try
                    {
                        var booking = await bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid());
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

            var allBookings = env.AllBookings();
            var updatedEvent = env.FindEvent(eventItem.Id);

            Assert.NotNull(updatedEvent);
            Assert.Equal(5, successfulBookingIds.Count);
            Assert.Equal(5, successfulBookingIds.Distinct().Count());
            Assert.Equal(5, allBookings.Count);
            Assert.Equal(0, updatedEvent.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldGenerateUniqueIds_UnderConcurrentLoad()
        {
            using var env = TestHelpers.Create();

            int seats = 10;

            var eventItem = TestHelpers.CreateTestEvent(seats);

            env.SeedEvent(eventItem);

            var tasks = Enumerable.Range(0, seats)
                    .Select(_ => Task.Run(async () =>
                    {
                        using var scope = env.CreateScope();

                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                        return await bookingService.CreateBookingAsync(eventItem.Id, Guid.NewGuid());
                    }));

            var bookings = await Task.WhenAll(tasks);

            Assert.Equal(seats, bookings.Length);
            Assert.Equal(seats, bookings.Select(b => b.Id).Distinct().Count());

            var allBookings = env.AllBookings();

            Assert.Equal(seats, allBookings.Count);
            Assert.Equal(seats, allBookings.Select(b => b.Id).Distinct().Count());
        }
    }
}
