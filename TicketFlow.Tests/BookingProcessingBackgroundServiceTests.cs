using Microsoft.Extensions.Logging;
using Moq;
using TicketFlow.Models;
using TicketFlow.Models.Store;
using TicketFlow.Services.Background;

namespace TicketFlow.Tests
{
    public class BookingProcessingBackgroundServiceTests
    {
        private readonly Mock<ILogger<BookingProcessingBackgroundService>> _loggerMock = new();

        [Fact]
        public async Task ExecuteAsync_ShouldConvertPendingToConfirmed_AndSetProcessedAt()
        {

            var bookingStore = new InMemoryBookingStore();
            var booking = new Booking(Guid.NewGuid());
            await bookingStore.AddAsync(booking);

            var service = new BookingProcessingBackgroundService(bookingStore, _loggerMock.Object);
            using var cts = new CancellationTokenSource();

            var backgroundTask = service.StartAsync(cts.Token);

            await Task.Delay(2500);
            await cts.CancelAsync();

            try
            {
                await backgroundTask;
            }
            catch (OperationCanceledException)
            {
            }

            // Assert
            var processedBooking = (await bookingStore.GetAllAsync()).First();

            Assert.Equal(BookingStatus.Confirmed, processedBooking.Status);
            Assert.NotNull(processedBooking.ProcessedAt);
            Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldIgnoreAlreadyConfirmedBookings()
        {
            var bookingStore = new InMemoryBookingStore();

            var booking = new Booking(Guid.NewGuid())
            {
                Status = BookingStatus.Confirmed,
                ProcessedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var originalProcessedAt = booking.ProcessedAt;

            await bookingStore.AddAsync(booking);

            var service = new BookingProcessingBackgroundService(bookingStore, _loggerMock.Object);
            using var cts = new CancellationTokenSource();

            var backgroundTask = service.StartAsync(cts.Token);
            await Task.Delay(500);

            await cts.CancelAsync();
            try { await backgroundTask; } catch (OperationCanceledException) { }

            var resultBooking = (await bookingStore.GetAllAsync()).First();

            Assert.Equal(BookingStatus.Confirmed, resultBooking.Status);
            Assert.Equal(originalProcessedAt, resultBooking.ProcessedAt);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldConvertPendingToRejected_WhenBusinessRulesDictate()
        {

            var bookingStore = new InMemoryBookingStore();
            var rejectedEventId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var booking = new Booking(rejectedEventId);
            await bookingStore.AddAsync(booking);

            var service = new BookingProcessingBackgroundService(bookingStore, _loggerMock.Object);
            using var cts = new CancellationTokenSource();

            var backgroundTask = service.StartAsync(cts.Token);

            await Task.Delay(2500);

            await cts.CancelAsync();
            try { await backgroundTask; } catch (OperationCanceledException) { }

            var processedBooking = (await bookingStore.GetAllAsync()).First();

            Assert.Equal(BookingStatus.Rejected, processedBooking.Status);
            Assert.NotNull(processedBooking.ProcessedAt);
            Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
        }
    }
}

