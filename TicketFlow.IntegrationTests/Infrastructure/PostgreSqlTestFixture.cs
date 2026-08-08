using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using TicketFlow.Application.DependencyInjection;
using TicketFlow.Infrastructure.DependencyInjection;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Infrastructure
{
    public class PostgreSqlTestFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("ticketflow_tests")
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

        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            return new AppDbContext(options);
        }

        public ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "test-secret-test-secret-test-secret-32bytes",
                    ["Jwt:Issuer"] = "TicketFlow.IntegrationTests",
                    ["Jwt:Audience"] = "TicketFlow.IntegrationTests",
                    ["Jwt:ExpirationMinutes"] = "60"
                })
                .Build();

            services.AddInfrastructureServices(ConnectionString, configuration);
            services.AddApplicationServices();

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