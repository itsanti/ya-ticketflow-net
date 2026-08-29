using Confluent.Kafka;
using System.Text.Json;
using TicketFlow.Bookings.Application.Abstractions;
using TicketFlow.Contracts;

namespace TicketFlow.Bookings.Infrastructure.Messaging
{
    public class KafkaBookingConfirmedPublisher : IBookingConfirmedPublisher, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public KafkaBookingConfirmedPublisher(IProducer<string, string> producer)
        {
            _producer = producer;
        }

        public async Task PublishAsync(BookingConfirmedEvent bookingConfirmedEvent, CancellationToken ct = default)
        {
            var message = new Message<string, string>
            {
                Key = bookingConfirmedEvent.EventId.ToString(),
                Value = JsonSerializer.Serialize(bookingConfirmedEvent)
            };

            await _producer.ProduceAsync(KafkaTopics.BookingConfirmed, message, ct);
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
    }
}
