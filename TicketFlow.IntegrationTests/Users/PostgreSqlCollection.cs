namespace TicketFlow.IntegrationTests.Users
{
    [CollectionDefinition("Users PostgreSql collection", DisableParallelization = true)]
    public class PostgreSqlCollection : ICollectionFixture<PostgreSqlTestFixture>
    {
    }
}
