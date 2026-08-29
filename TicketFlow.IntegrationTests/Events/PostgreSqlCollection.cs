namespace TicketFlow.IntegrationTests.Events
{
    [CollectionDefinition("Events PostgreSql collection", DisableParallelization = true)]
    public class PostgreSqlCollection : ICollectionFixture<PostgreSqlTestFixture>
    {
    }
}
