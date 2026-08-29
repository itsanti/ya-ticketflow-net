namespace TicketFlow.IntegrationTests.Bookings
{
    [CollectionDefinition("Bookings PostgreSql collection", DisableParallelization = true)]
    public class PostgreSqlCollection : ICollectionFixture<PostgreSqlTestFixture>
    {
    }
}
