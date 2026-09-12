using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Infrastructure.Caching;

namespace TicketFlow.Tests.Events
{
    public class RedisCacheServiceTests
    {
        private static readonly EventInfoDto SampleDto = new()
        {
            Id = Guid.NewGuid(),
            Title = "Кешируемое событие",
            Description = "Для тестов кеша",
            StartAt = new DateTime(2026, 09, 01, 19, 0, 0),
            EndAt = new DateTime(2026, 09, 01, 21, 0, 0),
            TotalSeats = 10,
            AvailableSeats = 4
        };

        private static (RedisCacheService Cache, Mock<IDatabase> Db, Mock<ILogger<RedisCacheService>> Logger) CreateCache()
        {
            var db = new Mock<IDatabase>();
            var redis = new Mock<IConnectionMultiplexer>();
            var logger = new Mock<ILogger<RedisCacheService>>();

            redis
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(db.Object);

            return (new RedisCacheService(redis.Object, logger.Object), db, logger);
        }

        private static (RedisCacheService Cache, Mock<ILogger<RedisCacheService>> Logger) CreateBrokenCache()
        {
            var redis = new Mock<IConnectionMultiplexer>();
            var logger = new Mock<ILogger<RedisCacheService>>();

            redis
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Throws(RedisUnavailable());

            return (new RedisCacheService(redis.Object, logger.Object), logger);
        }

        private static Exception RedisCommandFailed() =>
            new TimeoutException("The timeout was reached before the message could be written");

        private static Exception RedisUnavailable() =>
            new InvalidOperationException("No connection is available to service this operation");

        // Moq не умеет мокать extension-методы ILogger, поэтому проверяем вызов базового Log.
        private static void VerifyWarningLogged(Mock<ILogger<RedisCacheService>> logger) =>
            logger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        [Fact]
        public async Task GetAsync_ShouldReturnValue_WhenKeyExists()
        {
            var (cache, db, _) = CreateCache();

            db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(JsonSerializer.Serialize(SampleDto));

            var result = await cache.GetAsync<EventInfoDto>("event:1");

            Assert.NotNull(result);
            Assert.Equal(SampleDto.Title, result.Title);
            Assert.Equal(SampleDto.AvailableSeats, result.AvailableSeats);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDefault_WhenKeyDoesNotExist()
        {
            var (cache, db, _) = CreateCache();

            db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            var result = await cache.GetAsync<EventInfoDto>("event:1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDefault_AndLogWarning_WhenRedisUnavailable()
        {
            var (cache, db, logger) = CreateCache();

            db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(RedisCommandFailed());

            var result = await cache.GetAsync<EventInfoDto>("event:1");

            // Для вызывающего кода это неотличимо от промаха — он просто пойдёт в базу.
            Assert.Null(result);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDefault_WhenCachedValueIsNotValidJson()
        {
            var (cache, db, logger) = CreateCache();

            db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync("{not-valid-json");

            var result = await cache.GetAsync<EventInfoDto>("event:1");

            Assert.Null(result);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public async Task SetAsync_ShouldNotThrow_AndLogWarning_WhenRedisUnavailable()
        {
            var (cache, logger) = CreateBrokenCache();

            var exception = await Record.ExceptionAsync(
                () => cache.SetAsync("event:1", SampleDto, TimeSpan.FromSeconds(60)));

            Assert.Null(exception);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public async Task RemoveAsync_ShouldNotThrow_AndLogWarning_WhenRedisUnavailable()
        {
            var (cache, db, logger) = CreateCache();

            db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(RedisCommandFailed());

            var exception = await Record.ExceptionAsync(() => cache.RemoveAsync("event:1"));

            Assert.Null(exception);
            VerifyWarningLogged(logger);
        }

        [Fact]
        public async Task Operations_ShouldNotThrow_WhenConnectionItselfIsBroken()
        {
            var (cache, _) = CreateBrokenCache();

            Assert.Null(await cache.GetAsync<EventInfoDto>("event:1"));
            Assert.Null(await Record.ExceptionAsync(
                () => cache.SetAsync("event:1", SampleDto, TimeSpan.FromSeconds(60))));
            Assert.Null(await Record.ExceptionAsync(() => cache.RemoveAsync("event:1")));
        }
    }
}
