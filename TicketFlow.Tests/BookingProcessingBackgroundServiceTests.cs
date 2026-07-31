using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketFlow.Application.Services.Background;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Tests
{
    public class BookingProcessingBackgroundServiceTests
    {
        private readonly Mock<ILogger<BookingProcessingBackgroundService>> _loggerMock = new();

        private BookingProcessingBackgroundService CreateBackgroundService(TestEnvironment env)
        {
            var scopeFactory = env.Provider.GetRequiredService<IServiceScopeFactory>();

            return new BookingProcessingBackgroundService(
                scopeFactory,
                _loggerMock.Object);
        }

        private static async Task RunBackgroundServiceForAsync(
            BookingProcessingBackgroundService service,
            int milliseconds = 3000)
        {
            await service.StartAsync(CancellationToken.None);

            try
            {
                await Task.Delay(milliseconds);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ShouldConvertPendingToConfirmed_AndSetProcessedAt()
        {
            using var env = TestHelpers.Create();

            var eventItem = TestHelpers.CreateTestEvent(2);
            var booking = new Booking(eventItem.Id);

            env.SeedEvent(eventItem);
            env.SeedBooking(booking);

            var service = CreateBackgroundService(env);

            await RunBackgroundServiceForAsync(service);

            var processedBooking = env.FindBooking(booking.Id);

            Assert.NotNull(processedBooking);
            Assert.Equal(BookingStatus.Confirmed, processedBooking.Status);
            Assert.NotNull(processedBooking.ProcessedAt);
            Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldIgnoreAlreadyConfirmedBookings()
        {
            using var env = TestHelpers.Create();

            var booking = new Booking(Guid.NewGuid())
            {
                Status = BookingStatus.Confirmed,
                ProcessedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var originalProcessedAt = booking.ProcessedAt;

            env.SeedBooking(booking);

            var service = CreateBackgroundService(env);

            await RunBackgroundServiceForAsync(service, milliseconds: 1000);

            var resultBooking = env.FindBooking(booking.Id);

            Assert.NotNull(resultBooking);
            Assert.Equal(BookingStatus.Confirmed, resultBooking.Status);
            Assert.Equal(originalProcessedAt, resultBooking.ProcessedAt);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldConvertPendingToRejected_WhenEventDoesNotExist()
        {
            using var env = TestHelpers.Create();

            var fakeEventId = Guid.NewGuid();
            var booking = new Booking(fakeEventId);

            env.SeedBooking(booking);

            var service = CreateBackgroundService(env);

            await RunBackgroundServiceForAsync(service);

            var processedBooking = env.FindBooking(booking.Id);

            Assert.NotNull(processedBooking);
            Assert.Equal(BookingStatus.Rejected, processedBooking.Status);
            Assert.NotNull(processedBooking.ProcessedAt);
            Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
        }

    }
}
