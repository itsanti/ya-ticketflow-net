using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TicketFlow.Contracts;
using TicketFlow.Events.Application.Caching;
using TicketFlow.Events.Infrastructure.Messaging;

namespace TicketFlow.Tests.Events
{
    public class BookingConfirmedConsumerTests
    {
        private static BookingConfirmedConsumer CreateConsumer(TestEnvironment env)
        {
            var options = Options.Create(new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroup = "events-service-tests"
            });

            return new BookingConfirmedConsumer(
                options,
                env.Provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<BookingConfirmedConsumer>.Instance);
        }

        private static string Serialize(BookingConfirmedEvent bookingConfirmedEvent) =>
            JsonSerializer.Serialize(bookingConfirmedEvent);

        [Fact]
        public async Task HandleMessageAsync_ShouldReserveSeat_WhenEventExistsAndHasSeats()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var eventItem = TestHelpers.CreateTestEvent(5);
            env.SeedEvent(eventItem);

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), eventItem.Id, Guid.NewGuid(), 1, DateTime.UtcNow);

            await consumer.HandleMessageAsync(Serialize(message), CancellationToken.None);

            Assert.Equal(4, env.FindEvent(eventItem.Id)!.AvailableSeats);

            env.EventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldSkip_WhenEventNotFound()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

            await consumer.HandleMessageAsync(Serialize(message), CancellationToken.None);

            env.EventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldSkip_WhenNoAvailableSeats()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var eventItem = TestHelpers.CreateTestEvent(1);
            Assert.True(eventItem.TryReserveSeats(1));
            env.SeedEvent(eventItem);

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), eventItem.Id, Guid.NewGuid(), 1, DateTime.UtcNow);

            await consumer.HandleMessageAsync(Serialize(message), CancellationToken.None);

            Assert.Equal(0, env.FindEvent(eventItem.Id)!.AvailableSeats);

            env.EventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldSkip_WhenPayloadIsInvalidJson()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var exception = await Record.ExceptionAsync(() =>
                consumer.HandleMessageAsync("{not-valid-json", CancellationToken.None));

            Assert.Null(exception);

            env.EventRepository.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldSkip_WhenBookingAlreadyProcessed()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var eventItem = TestHelpers.CreateTestEvent(5);
            env.SeedEvent(eventItem);

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), eventItem.Id, Guid.NewGuid(), 1, DateTime.UtcNow);
            var payload = Serialize(message);

            await consumer.HandleMessageAsync(payload, CancellationToken.None);
            await consumer.HandleMessageAsync(payload, CancellationToken.None);

            // Место должно уменьшиться только один раз — второй (дублирующий) вызов пропущен.
            Assert.Equal(4, env.FindEvent(eventItem.Id)!.AvailableSeats);

            env.EventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldInvalidateEventCache_WhenSeatReserved()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var eventItem = TestHelpers.CreateTestEvent(5);
            env.SeedEvent(eventItem);

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), eventItem.Id, Guid.NewGuid(), 1, DateTime.UtcNow);

            await consumer.HandleMessageAsync(Serialize(message), CancellationToken.None);

            env.CacheService.Verify(
                c => c.RemoveAsync(CacheKeys.EventKey(eventItem.Id), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldNotInvalidateCache_WhenNoAvailableSeats()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            var eventItem = TestHelpers.CreateTestEvent(1);
            Assert.True(eventItem.TryReserveSeats(1));
            env.SeedEvent(eventItem);

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), eventItem.Id, Guid.NewGuid(), 1, DateTime.UtcNow);

            await consumer.HandleMessageAsync(Serialize(message), CancellationToken.None);

            env.CacheService.Verify(
                c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ShouldNotThrow_WhenRepositoryThrows()
        {
            using var env = TestHelpers.Create();
            var consumer = CreateConsumer(env);

            env.EventRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB is unavailable"));

            var message = new BookingConfirmedEvent(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

            var exception = await Record.ExceptionAsync(() =>
                consumer.HandleMessageAsync(Serialize(message), CancellationToken.None));

            Assert.Null(exception);
        }
    }
}
