using TicketFlow.DataAccess.Repositories;
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
                    IReadOnlyList<Guid> pendingBookingIds;
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                        pendingBookingIds = await bookingRepository.GetPendingIdsAsync(stoppingToken);
                    }

                    var tasks = pendingBookingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
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

                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return;

                var eventItem = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                if (eventItem == null)
                {
                    booking.Reject();
                    await bookingRepository.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning("Event not found. Booking {BookingId} rejected.", booking.Id);
                    return;
                }

                booking.Confirm();
                await bookingRepository.SaveChangesAsync(stoppingToken);

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
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking != null)
                {
                    booking.Reject();
                    var eventItem = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                    if (eventItem != null)
                    {
                        eventItem.ReleaseSeats();
                    }
                    await bookingRepository.SaveChangesAsync(stoppingToken);

                    _logger.LogError(ex, "Booking {BookingId} rejected due to processing error", bookingId);
                }

            }
        }
    }
}
