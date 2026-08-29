using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using TicketFlow.Bookings.Application.Abstractions;
using TicketFlow.Bookings.Application.DependencyInjection;
using TicketFlow.Bookings.Infrastructure.DependencyInjection;
using TicketFlow.Bookings.Infrastructure.Persistence;
using TicketFlow.Contracts;

namespace TicketFlow.IntegrationTests.Bookings
{
    public class PostgreSqlTestFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("ticketflow_bookings_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        public BookingsDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<BookingsDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            return new BookingsDbContext(options);
        }

        public ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Booking:MaxActiveBookingsPerUser"] = "10"
                })
                .Build();

            services.AddInfrastructureServices(ConnectionString, configuration);
            services.AddApplicationServices(configuration);

            // Kafka-паблишер заменён на заглушку: рядом нет брокера, а здесь проверяется
            // только смена статуса брони в БД, не доставка сообщения.
            var publisherMock = new Mock<IBookingConfirmedPublisher>();
            publisherMock
                .Setup(p => p.PublishAsync(It.IsAny<BookingConfirmedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(publisherMock.Object);

            return services.BuildServiceProvider();
        }

        public async Task ResetDatabaseAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }
    }
}
