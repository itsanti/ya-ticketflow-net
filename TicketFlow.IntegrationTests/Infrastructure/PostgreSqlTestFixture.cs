using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TicketFlow.DataAccess;

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

        public async Task ResetDatabaseAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }
    }
}