using Microsoft.EntityFrameworkCore;
using TicketFlow.DataAccess;
using TicketFlow.Models;

namespace TicketFlow.Services.Background
{
    public class BookingProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingBackgroundService> logger
    ) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<BookingProcessingBackgroundService> _logger = logger;

        private static readonly int _processingDelay = 2000;
        private static readonly int _pollingInterval = 5000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking processing background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<Guid> pendingBookingIds;
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        pendingBookingIds = await context.Bookings
                            .AsNoTracking()
                            .Where(b => b.Status == BookingStatus.Pending)
                            .Select(b => b.Id)
                            .ToListAsync(stoppingToken);
                    }

                    var tasks = pendingBookingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during booking polling.");
                }

                try
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

            }

            _logger.LogInformation("Booking processing background service is stopping.");
        }

        private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(_processingDelay, stoppingToken);

                _logger.LogInformation("Processing booking with ID {BookingId}", bookingId);

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return;

                var eventItem = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);

                if (eventItem == null)
                {
                    booking.Reject();
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning("Event not found. Booking {BookingId} rejected.", booking.Id);
                    return;
                }

                booking.Confirm();
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Successfully processed booking with ID {BookingId}", booking.Id);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Processing canceled for booking {BookingId}", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing booking with ID {BookingId}", bookingId);

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
                if (booking != null)
                {
                    booking.Reject();
                    var eventItem = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);
                    if (eventItem != null)
                    {
                        eventItem.ReleaseSeats();
                    }
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogError(ex, "Booking {BookingId} rejected due to processing error", bookingId);
                }

            }
        }
    }
}
