using System.Text.Json;
using Confluent.Kafka;
using Moq;
using TicketFlow.Bookings.Infrastructure.Messaging;
using TicketFlow.Contracts;

namespace TicketFlow.Tests.Bookings
{
    public class KafkaBookingConfirmedPublisherTests
    {
        private static readonly BookingConfirmedEvent SampleEvent = new(
            BookingId: Guid.NewGuid(),
            EventId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            SeatsCount: 1,
            ConfirmedAtUtc: DateTime.UtcNow);

        [Fact]
        public async Task PublishAsync_ShouldSendToBookingConfirmedTopic()
        {
            var producerMock = new Mock<IProducer<string, string>>();
            producerMock
                .Setup(p => p.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<Message<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryResult<string, string>());

            var publisher = new KafkaBookingConfirmedPublisher(producerMock.Object);

            await publisher.PublishAsync(SampleEvent);

            producerMock.Verify(p => p.ProduceAsync(
                KafkaTopics.BookingConfirmed,
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishAsync_ShouldUseEventIdAsMessageKey()
        {
            Message<string, string>? capturedMessage = null;

            var producerMock = new Mock<IProducer<string, string>>();
            producerMock
                .Setup(p => p.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<Message<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, Message<string, string>, CancellationToken>((_, message, _) => capturedMessage = message)
                .ReturnsAsync(new DeliveryResult<string, string>());

            var publisher = new KafkaBookingConfirmedPublisher(producerMock.Object);

            await publisher.PublishAsync(SampleEvent);

            Assert.NotNull(capturedMessage);
            Assert.Equal(SampleEvent.EventId.ToString(), capturedMessage.Key);
        }

        [Fact]
        public async Task PublishAsync_ShouldSerializeEventAsJson_RoundTrippable()
        {
            Message<string, string>? capturedMessage = null;

            var producerMock = new Mock<IProducer<string, string>>();
            producerMock
                .Setup(p => p.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<Message<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, Message<string, string>, CancellationToken>((_, message, _) => capturedMessage = message)
                .ReturnsAsync(new DeliveryResult<string, string>());

            var publisher = new KafkaBookingConfirmedPublisher(producerMock.Object);

            await publisher.PublishAsync(SampleEvent);

            var deserialized = JsonSerializer.Deserialize<BookingConfirmedEvent>(capturedMessage!.Value);

            Assert.Equal(SampleEvent, deserialized);
        }

        [Fact]
        public void Dispose_ShouldFlushAndDisposeProducer()
        {
            var producerMock = new Mock<IProducer<string, string>>();
            producerMock.Setup(p => p.Flush(It.IsAny<TimeSpan>())).Returns(0);

            var publisher = new KafkaBookingConfirmedPublisher(producerMock.Object);

            publisher.Dispose();

            producerMock.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
            producerMock.Verify(p => p.Dispose(), Times.Once);
        }
    }
}
