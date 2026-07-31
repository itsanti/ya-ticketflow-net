using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicketFlow.Application.Services.Background;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.IntegrationTests.Infrastructure;

namespace TicketFlow.IntegrationTests.Services
{
    [Collection("PostgreSql collection")]
    public class BookingProcessingBackgroundServiceTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public BookingProcessingBackgroundServiceTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldPersistConfirmedStatus_WhenEventExists()
        {
            await _fixture.ResetDatabaseAsync();

            Booking booking;

            await using (var context = _fixture.CreateContext())
            {
                var eventItem = Event.Create(
                    "Tech Conference",
                    "Description",
                    DateTime.UtcNow.AddDays(1),
                    DateTime.UtcNow.AddDays(2),
                    10);

                booking = new Booking(eventItem.Id);

                await context.Events.AddAsync(eventItem);
                await context.Bookings.AddAsync(booking);
                await context.SaveChangesAsync();
            }

            await using var serviceProvider = _fixture.CreateServiceProvider();

            var service = new BookingProcessingBackgroundService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<BookingProcessingBackgroundService>.Instance);

            await service.StartAsync(CancellationToken.None);

            try
            {
                // Первый цикл опроса стартует сразу, обработка одной заявки занимает ~2 секунды.
                await Task.Delay(5000);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }

            await using var verificationContext = _fixture.CreateContext();

            var processedBooking = await verificationContext.Bookings
                .AsNoTracking()
                .FirstAsync(b => b.Id == booking.Id);

            Assert.Equal(BookingStatus.Confirmed, processedBooking.Status);
            Assert.NotNull(processedBooking.ProcessedAt);
        }
    }
}
