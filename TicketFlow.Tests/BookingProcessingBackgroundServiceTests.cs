using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketFlow.DataAccess;
using TicketFlow.Models;
using TicketFlow.Services.Background;

namespace TicketFlow.Tests
{
    public class BookingProcessingBackgroundServiceTests
    {
        private readonly Mock<ILogger<BookingProcessingBackgroundService>> _loggerMock = new();

        private BookingProcessingBackgroundService CreateBackgroundService(ServiceProvider serviceProvider)
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

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
            using var serviceProvider = TestHelpers.Create();

            var eventItem = TestHelpers.CreateTestEvent(2);
            var booking = new Booking(eventItem.Id);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Events.AddAsync(eventItem);
                await context.Bookings.AddAsync(booking);
                await context.SaveChangesAsync();
            }

            var service = CreateBackgroundService(serviceProvider);

            await RunBackgroundServiceForAsync(service);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var processedBooking = await context.Bookings
                    .AsNoTracking()
                    .FirstAsync(b => b.Id == booking.Id);

                Assert.Equal(BookingStatus.Confirmed, processedBooking.Status);
                Assert.NotNull(processedBooking.ProcessedAt);
                Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ShouldIgnoreAlreadyConfirmedBookings()
        {
            using var serviceProvider = TestHelpers.Create();

            var booking = new Booking(Guid.NewGuid())
            {
                Status = BookingStatus.Confirmed,
                ProcessedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var originalProcessedAt = booking.ProcessedAt;

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Bookings.AddAsync(booking);
                await context.SaveChangesAsync();
            }

            var service = CreateBackgroundService(serviceProvider);

            await RunBackgroundServiceForAsync(service, milliseconds: 1000);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var resultBooking = await context.Bookings
                    .AsNoTracking()
                    .FirstAsync(b => b.Id == booking.Id);

                Assert.Equal(BookingStatus.Confirmed, resultBooking.Status);
                Assert.Equal(originalProcessedAt, resultBooking.ProcessedAt);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ShouldConvertPendingToRejected_WhenEventDoesNotExist()
        {
            using var serviceProvider = TestHelpers.Create();

            var fakeEventId = Guid.NewGuid();
            var booking = new Booking(fakeEventId);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Bookings.AddAsync(booking);
                await context.SaveChangesAsync();
            }

            var service = CreateBackgroundService(serviceProvider);

            await RunBackgroundServiceForAsync(service);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var processedBooking = await context.Bookings
                    .AsNoTracking()
                    .FirstAsync(b => b.Id == booking.Id);

                Assert.Equal(BookingStatus.Rejected, processedBooking.Status);
                Assert.NotNull(processedBooking.ProcessedAt);
                Assert.True(processedBooking.ProcessedAt <= DateTime.UtcNow);
            }
        }

    }
}

