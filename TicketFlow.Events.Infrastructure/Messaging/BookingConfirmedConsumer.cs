using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TicketFlow.Contracts;
using TicketFlow.Events.Application.Abstractions;

namespace TicketFlow.Events.Infrastructure.Messaging
{
    public class BookingConfirmedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<BookingConfirmedConsumer> logger
    ) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                GroupId = options.Value.ConsumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(KafkaTopics.BookingConfirmed);

            logger.LogInformation("Kafka consumer subscribed to '{Topic}'.", KafkaTopics.BookingConfirmed);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<string, string>? result;

                    try
                    {
                        result = consumer.Consume(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ConsumeException ex)
                    {
                        logger.LogError(ex, "Error consuming message from Kafka.");
                        continue;
                    }

                    if (result?.Message == null)
                    {
                        continue;
                    }

                    await HandleMessageAsync(result.Message.Value, stoppingToken);
                }
            }
            finally
            {
                consumer.Close();
            }
        }

        private async Task HandleMessageAsync(string payload, CancellationToken ct)
        {
            BookingConfirmedEvent? bookingConfirmedEvent;

            try
            {
                bookingConfirmedEvent = JsonSerializer.Deserialize<BookingConfirmedEvent>(payload);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize BookingConfirmed message: {Payload}", payload);
                return;
            }

            if (bookingConfirmedEvent == null)
            {
                logger.LogWarning("Received empty BookingConfirmed message.");
                return;
            }

            try
            {
                // Consumer — singleton, IEventRepository/DbContext — scoped: свой scope на каждое сообщение.
                using var scope = scopeFactory.CreateScope();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var eventItem = await eventRepository.GetByIdAsync(bookingConfirmedEvent.EventId, ct);

                if (eventItem == null)
                {
                    logger.LogWarning("Event {EventId} not found for booking {BookingId} — skipping.",
                        bookingConfirmedEvent.EventId, bookingConfirmedEvent.BookingId);
                    return;
                }

                if (!eventItem.TryReserveSeats(bookingConfirmedEvent.SeatsCount))
                {
                    logger.LogWarning("No available seats for event {EventId}, booking {BookingId} — skipping.",
                        bookingConfirmedEvent.EventId, bookingConfirmedEvent.BookingId);
                    return;
                }

                await eventRepository.SaveChangesAsync(ct);

                logger.LogInformation("Reserved {SeatsCount} seat(s) for event {EventId} from booking {BookingId}.",
                    bookingConfirmedEvent.SeatsCount, bookingConfirmedEvent.EventId, bookingConfirmedEvent.BookingId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing BookingConfirmed for booking {BookingId} — message skipped.",
                    bookingConfirmedEvent.BookingId);
            }
        }
    }
}
