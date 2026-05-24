using TicketFlow.Models;
using TicketFlow.Models.Store;

namespace TicketFlow.Services.Background
{
    public class BookingProcessingBackgroundService(
        IInMemoryStore<Booking> store,
        ILogger<BookingProcessingBackgroundService> logger
    ) : BackgroundService
    {
        private readonly IInMemoryStore<Booking> _store = store;
        private readonly ILogger<BookingProcessingBackgroundService> _logger = logger;

        private readonly int _processingDelay = 2000;
        private readonly int _pollingInterval = 5000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking processing background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var bookings = await _store.FindAsync(b => b.Status == BookingStatus.Pending);

                    foreach (Booking booking in bookings)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        try
                        {
                            _logger.LogInformation("Processing booking with ID {BookingId}", booking.Id);

                            await Task.Delay(_processingDelay, stoppingToken);

                            booking.Status = DetermineTargetStatus(booking);
                            booking.ProcessedAt = DateTime.UtcNow;

                            await _store.UpdateAsync(booking);

                            _logger.LogInformation("Successfully processed booking with ID {BookingId}", booking.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred while processing booking with ID {BookingId}", booking.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during booking store polling.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("Booking processing background service is stopping.");
        }

        private static BookingStatus DetermineTargetStatus(Booking booking)
        {
            // TODO: Заглушка для будущего спринта.
            if (booking.EventId == Guid.Parse("00000000-0000-0000-0000-000000000001"))
            {
                return BookingStatus.Rejected;
            }

            return BookingStatus.Confirmed;
        }
    }
}
