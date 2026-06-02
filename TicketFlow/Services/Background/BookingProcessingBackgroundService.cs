using TicketFlow.Models;
using TicketFlow.Models.Store;

namespace TicketFlow.Services.Background
{
    public class BookingProcessingBackgroundService(
        IInMemoryStore<Booking> store,
        IInMemoryStore<Event> eventStore,
        ILogger<BookingProcessingBackgroundService> logger
    ) : BackgroundService
    {
        private readonly IInMemoryStore<Booking> _store = store;
        private readonly IInMemoryStore<Event> _eventStore = eventStore;
        private readonly ILogger<BookingProcessingBackgroundService> _logger = logger;

        private readonly int _processingDelay = 2000;

        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking processing background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var bookings = await _store.FindAsync(b => b.Status == BookingStatus.Pending);

                    var tasks = bookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during booking store polling.");
                }
            }

            _logger.LogInformation("Booking processing background service is stopping.");
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            Event? eventItem = null;

            await Task.Delay(_processingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                _logger.LogInformation("Processing booking with ID {BookingId}", booking.Id);

                var events_ = await _eventStore.FindAsync(e => e.Id == booking.EventId);
                eventItem = events_.FirstOrDefault();

                if (eventItem == null)
                {
                    booking.Reject();
                    await _store.UpdateAsync(booking);
                    _logger.LogWarning("Event not found. Booking {BookingId} rejected.", booking.Id);
                    return;
                }

                booking.Confirm();
                await _store.UpdateAsync(booking);

                _logger.LogInformation("Successfully processed booking with ID {BookingId}", booking.Id);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processing canceled for booking {BookingId}", booking.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing booking with ID {BookingId}", booking.Id);
                booking.Reject();
                await _store.UpdateAsync(booking);
                if (eventItem != null)
                {
                    eventItem.ReleaseSeats();
                    await _eventStore.UpdateAsync(eventItem);
                }
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
    }
}
