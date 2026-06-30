using Microsoft.EntityFrameworkCore;
using TicketFlow.IntegrationTests.Infrastructure;

namespace TicketFlow.IntegrationTests.Migrations
{
    [Collection("PostgreSql collection")]
    public class MigrationTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public MigrationTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Migrate_ShouldCreateRequiredTables()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();

            var tables = await context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT tablename
                    FROM pg_tables
                    WHERE schemaname = 'public'
                    """)
                .ToListAsync();

            Assert.Contains("events", tables);
            Assert.Contains("bookings", tables);
            Assert.Contains("__EFMigrationsHistory", tables);
        }

        [Fact]
        public async Task Migrate_ShouldCreateForeignKey_FromBookingsToEvents()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();

            var foreignKeyCount = await context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*)::int AS "Value"
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                      ON tc.constraint_name = kcu.constraint_name
                     AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage ccu
                      ON ccu.constraint_name = tc.constraint_name
                     AND ccu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                      AND tc.table_schema = 'public'
                      AND tc.table_name = 'bookings'
                      AND kcu.column_name = 'event_id'
                      AND ccu.table_schema = 'public'
                      AND ccu.table_name = 'events'
                      AND ccu.column_name = 'id'
                    """)
                .SingleAsync();

            Assert.Equal(1, foreignKeyCount);
        }
    }
}
