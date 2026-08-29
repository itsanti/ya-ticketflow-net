using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.IntegrationTests.Infrastructure;
using TicketFlow.Users.Domain.Enums;

namespace TicketFlow.IntegrationTests.Events
{
    [Collection("Events PostgreSql collection")]
    public class AuthHttpTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public AuthHttpTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateEvent_ShouldReturn403_WhenCalledByNonAdminUser()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            var userToken = TestTokenFactory.CreateToken(Guid.NewGuid(), "regular-user", UserRole.User);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var response = await client.PostAsJsonAsync("/events", new CreateEventDto
            {
                Title = "Some event",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                TotalSeats = 10
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
