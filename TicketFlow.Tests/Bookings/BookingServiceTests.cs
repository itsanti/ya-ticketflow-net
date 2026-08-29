using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketFlow.Bookings.Application.DTOs;
using TicketFlow.Bookings.Application.Services;
using TicketFlow.Bookings.Domain.Enums;
using TicketFlow.Bookings.Domain.Exceptions;

namespace TicketFlow.Tests.Bookings
{
    // Часть тестов монолитной версии удалена без замены: они проверяли поведение,
    // которого у Bookings больше нет (существование/старт события, места, блокировка
    // от овербукинга) — эта ответственность переехала в Events через Kafka.
    public class BookingServiceTests
    {
        [Fact]
        public async Task CreateBooking_ShouldReturnPendingBooking_WhenEventExists()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();

            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var booking = await bookingService.CreateBookingAsync(eventId, userId);

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
            var userId = Guid.NewGuid();
            var createdBooking = await bookingService.CreateBookingAsync(eventId, userId);

            var retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id, userId, UserRole.User);

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
                bookingService.GetBookingByIdAsync(Guid.NewGuid(), Guid.NewGuid(), UserRole.User));
        }

        [Fact]
        public async Task GetBooking_ShouldReflectStatusChange_AfterDatabaseUpdate()
        {
            using var env = TestHelpers.Create();

            BookingResponseDto booking;
            var userId = Guid.NewGuid();

            using (var createScope = env.CreateScope())
            {
                var bookingService = createScope.ServiceProvider.GetRequiredService<IBookingService>();

                booking = await bookingService.CreateBookingAsync(Guid.NewGuid(), userId);
            }

            var bookingToUpdate = env.FindBooking(booking.Id);

            Assert.NotNull(bookingToUpdate);

            bookingToUpdate.Status = BookingStatus.Confirmed;
            bookingToUpdate.ProcessedAt = DateTime.UtcNow;

            using (var verificationScope = env.CreateScope())
            {
                var bookingService = verificationScope.ServiceProvider.GetRequiredService<IBookingService>();

                var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id, userId, UserRole.User);

                Assert.Equal(nameof(BookingStatus.Confirmed), updatedBooking.Status);
                Assert.NotNull(updatedBooking.ProcessedAt);
            }
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldSaveChangesOnce_WhenBookingIsCreated()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await bookingService.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid());

            env.BookingRepository.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldGenerateUniqueIds_UnderConcurrentLoad()
        {
            using var env = TestHelpers.Create();

            var eventId = Guid.NewGuid();
            const int requestCount = 10;

            var tasks = Enumerable.Range(0, requestCount)
                    .Select(_ => Task.Run(async () =>
                    {
                        using var scope = env.CreateScope();

                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                        return await bookingService.CreateBookingAsync(eventId, Guid.NewGuid());
                    }));

            var bookings = await Task.WhenAll(tasks);

            Assert.Equal(requestCount, bookings.Length);
            Assert.Equal(requestCount, bookings.Select(b => b.Id).Distinct().Count());

            var allBookings = env.AllBookings();

            Assert.Equal(requestCount, allBookings.Count);
            Assert.Equal(requestCount, allBookings.Select(b => b.Id).Distinct().Count());
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldThrowBookingLimitExceededException_WhenUserReachesActiveBookingsLimit()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            for (int i = 0; i < 10; i++)
            {
                await bookingService.CreateBookingAsync(eventId, userId);
            }

            await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
                bookingService.CreateBookingAsync(eventId, userId));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldSucceed_WhenAnotherUserHasReachedTheirOwnLimit()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();
            var firstUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();

            for (int i = 0; i < 10; i++)
            {
                await bookingService.CreateBookingAsync(eventId, firstUserId);
            }

            var booking = await bookingService.CreateBookingAsync(eventId, secondUserId);

            Assert.Equal(nameof(BookingStatus.Pending), booking.Status);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldThrowForbiddenException_WhenNonOwnerNonAdminRequests()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var booking = await bookingService.CreateBookingAsync(Guid.NewGuid(), ownerId);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                bookingService.GetBookingByIdAsync(booking.Id, otherUserId, UserRole.User));
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenAdminRequestsOtherUserBooking()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();

            var booking = await bookingService.CreateBookingAsync(Guid.NewGuid(), ownerId);

            var retrievedBooking = await bookingService.GetBookingByIdAsync(booking.Id, adminId, UserRole.Admin);

            Assert.Equal(booking.Id, retrievedBooking.Id);
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldCancelBooking_WhenOwnerCancelsBeforeEventStart()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var userId = Guid.NewGuid();

            var booking = await bookingService.CreateBookingAsync(Guid.NewGuid(), userId);

            await bookingService.CancelBookingAsync(booking.Id, userId, UserRole.User);

            var storedBooking = env.FindBooking(booking.Id);

            Assert.NotNull(storedBooking);
            Assert.Equal(BookingStatus.Cancelled, storedBooking.Status);
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldThrowForbiddenException_WhenNonOwnerNonAdminCancels()
        {
            using var env = TestHelpers.Create();
            using var scope = env.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var booking = await bookingService.CreateBookingAsync(Guid.NewGuid(), ownerId);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                bookingService.CancelBookingAsync(booking.Id, otherUserId, UserRole.User));
        }
    }
}
