using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketFlow.Bookings.Application.DTOs;
using TicketFlow.IntegrationTests.Infrastructure;
using TicketFlow.Users.Domain.Enums;

namespace TicketFlow.IntegrationTests.Bookings
{
    [Collection("Bookings PostgreSql collection")]
    public class AuthHttpTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public AuthHttpTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetBooking_ShouldReturn401_WhenNoTokenProvided()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            var response = await client.GetAsync($"/bookings/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CancelBooking_ShouldReturn403_WhenCancellingOtherUsersBooking()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            // Bookings не проверяет существование события — случайный EventId достаточен.
            var ownerToken = TestTokenFactory.CreateToken(Guid.NewGuid(), "owner", UserRole.User);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var bookingResponse = await client.PostAsync($"/events/{Guid.NewGuid()}/book", content: null);
            bookingResponse.EnsureSuccessStatusCode();
            var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponseDto>();

            var otherUserToken = TestTokenFactory.CreateToken(Guid.NewGuid(), "other-user", UserRole.User);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherUserToken);

            var cancelResponse = await client.DeleteAsync($"/bookings/{booking!.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, cancelResponse.StatusCode);
        }
    }
}
