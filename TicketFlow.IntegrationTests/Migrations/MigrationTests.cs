using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TicketFlow.IntegrationTests.Infrastructure;

namespace TicketFlow.IntegrationTests.Migrations
{
    [Collection("PostgreSql collection")]
    public class MigrationTests
    {
        private const string InitialCreateMigration = "20260630124334_InitialCreate";
        private const string AddUsersAndBookingOwnershipMigration = "20260808191533_AddUsersAndBookingOwnership";
        private static readonly Guid SentinelUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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

        [Fact]
        public async Task Migrate_ShouldBackfillPreExistingBookingsWithSentinelUser_WhenUpgradingFromInitialCreate()
        {
            NpgsqlConnection.ClearAllPools();

            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            await using (var context = _fixture.CreateContext())
            {
                await context.Database.EnsureDeletedAsync();

                var migrator = context.Database.GetService<IMigrator>();

                await migrator.MigrateAsync(InitialCreateMigration);

                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO events (id, title, description, start_at, end_at, total_seats, available_seats)
                    VALUES ({eventId}, 'Legacy Event', NULL, now() + interval '1 day', now() + interval '2 day', 10, 9)
                    """);

                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO bookings (id, event_id, status, created_at, processed_at)
                    VALUES ({bookingId}, {eventId}, 'Pending', now(), NULL)
                    """);

                await migrator.MigrateAsync(AddUsersAndBookingOwnershipMigration);
            }

            await using var verifyContext = _fixture.CreateContext();

            var backfilledUserId = await verifyContext.Database
                .SqlQueryRaw<Guid>(
                    """
                    SELECT user_id AS "Value" FROM bookings WHERE id = @bookingId
                    """,
                    new NpgsqlParameter("bookingId", bookingId))
                .SingleAsync();

            Assert.Equal(SentinelUserId, backfilledUserId);

            var sentinelUserExists = await verifyContext.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*)::int AS "Value" FROM users WHERE id = @sentinelId AND login = 'legacy-system'
                    """,
                    new NpgsqlParameter("sentinelId", SentinelUserId))
                .SingleAsync();

            Assert.Equal(1, sentinelUserExists);
        }
    }
}
