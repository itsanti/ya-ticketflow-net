using Microsoft.Extensions.Logging;
using Moq;
using TicketFlow.Models;
using TicketFlow.Models.Store;
using TicketFlow.Services;
using TicketFlow.Services.Background;

namespace TicketFlow.Tests
{
    public class FailingBookingStore : InMemoryBookingStore
    {
        public override Task<Booking> UpdateAsync(Booking entity)
        {
            if (entity.Status == BookingStatus.Confirmed)
            {
                throw new Exception("Simulated database failure during confirmation.");
            }
            return base.UpdateAsync(entity);
        }
    }

    public class BookingProcessingBackgroundServiceTests
    {
        private readonly Mock<ILogger<BookingProcessingBackgroundService>> _loggerMock = new();

        [Fact]
        public async Task ExecuteAsync_ShouldConvertPendingToConfirmed_AndSetProcessedAt()
        {

            var bookingStore = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();

            var eventItem = TestHelpers.CreateTestEvent(2);

            await eventStore.AddAsync(eventItem);

            var booking = new Booking(eventItem.Id);
            await bookingStore.AddAsync(booking);

            var service = new BookingProcessingBackgroundService(bookingStore, eventStore, _loggerMock.Object);
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

            var processedBooking = (await bookingStore.GetAllAsync()).First();

            Assert.Equal(BookingStatus.Confirmed, processedBooking.Status);
            Assert.NotNull(processedBooking.ProcessedAt);
            Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldIgnoreAlreadyConfirmedBookings()
        {
            var bookingStore = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();

            var booking = new Booking(Guid.NewGuid())
            {
                Status = BookingStatus.Confirmed,
                ProcessedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var originalProcessedAt = booking.ProcessedAt;

            await bookingStore.AddAsync(booking);

            var service = new BookingProcessingBackgroundService(bookingStore, eventStore, _loggerMock.Object);
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
        public async Task ExecuteAsync_ShouldConvertPendingToRejected_WhenEventDoesNotExist()
        {
            var bookingStore = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();

            var fakeEventId = Guid.NewGuid();
            var booking = new Booking(fakeEventId);
            await bookingStore.AddAsync(booking);

            var service = new BookingProcessingBackgroundService(bookingStore, eventStore, _loggerMock.Object);
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

        [Fact]
        public async Task ExecuteAsync_ShouldRejectBookingAndReleaseSeats_WhenExceptionOccurs_AndAllowNewBooking()
        {
            var bookingStore = new FailingBookingStore();
            var eventStore = new InMemoryEventStore();
            var bookingService = new BookingService(bookingStore, eventStore);

            var eventItem = TestHelpers.CreateTestEvent(1);

            await eventStore.AddAsync(eventItem);

            var firstBooking = await bookingService.CreateBookingAsync(eventItem.Id);
            Assert.Equal(0, eventItem.AvailableSeats);

            var backgroundService = new BookingProcessingBackgroundService(bookingStore, eventStore, _loggerMock.Object);
            using var cts = new CancellationTokenSource();

            var backgroundTask = backgroundService.StartAsync(cts.Token);

            await Task.Delay(2500);

            await cts.CancelAsync();
            try { await backgroundTask; } catch (OperationCanceledException) { }

            var updatedEvent = (await eventStore.FindAsync(e => e.Id == eventItem.Id)).First();
            Assert.Equal(1, updatedEvent.AvailableSeats);

            var secondBooking = await bookingService.CreateBookingAsync(eventItem.Id);

            Assert.NotNull(secondBooking);
            Assert.Equal(BookingStatus.Pending, secondBooking.Status);
            Assert.Equal(0, updatedEvent.AvailableSeats);
        }
    }
}

