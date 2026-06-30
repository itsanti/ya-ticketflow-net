namespace TicketFlow.IntegrationTests.Infrastructure
{
    [CollectionDefinition("PostgreSql collection", DisableParallelization = true)]
    public class PostgreSqlCollection : ICollectionFixture<PostgreSqlTestFixture>
    {
    }
}