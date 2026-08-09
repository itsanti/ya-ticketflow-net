using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Abstractions;
using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Application.DTOs.Events;
using TicketFlow.Application.DTOs.Users;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.IntegrationTests.Infrastructure;

namespace TicketFlow.IntegrationTests.Http
{
    /// <summary>
    /// End-to-end HTTP tests over the real pipeline (routing, [Authorize], JwtBearer,
    /// GlobalExceptionHandlingMiddleware) — verifies status codes a unit test can't see,
    /// since those only exercise service-layer exceptions, not their HTTP mapping.
    /// </summary>
    [Collection("PostgreSql collection")]
    public class AuthorizationHttpTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public AuthorizationHttpTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static async Task<string> RegisterAndLoginAsync(HttpClient client)
        {
            var login = $"user-{Guid.NewGuid()}";
            const string password = "P@ssw0rd123";

            var registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterUserDto
            {
                Login = login,
                Password = password
            });
            registerResponse.EnsureSuccessStatusCode();

            return await LoginAsync(client, login, password);
        }

        /// <summary>
        /// /auth/register can only ever create role User (see RegisterUserDto) — Admins are
        /// created out-of-band (Program.cs "create-admin" command). Seed one directly here to
        /// test admin-only routes, then log in through the real HTTP endpoint like any user.
        /// </summary>
        private async Task<string> SeedAdminAndLoginAsync(CustomWebApplicationFactory factory, HttpClient client)
        {
            var login = $"admin-{Guid.NewGuid()}";
            const string password = "P@ssw0rd123";

            using (var scope = factory.Services.CreateScope())
            {
                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

                var admin = User.Create(login, hasher.Hash(password), UserRole.Admin);

                await userRepo.AddAsync(admin);
                await userRepo.SaveChangesAsync();
            }

            return await LoginAsync(client, login, password);
        }

        private static async Task<string> LoginAsync(HttpClient client, string login, string password)
        {
            var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginUserDto
            {
                Login = login,
                Password = password
            });
            loginResponse.EnsureSuccessStatusCode();

            var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            return auth!.Token;
        }

        private static void SetBearerToken(HttpClient client, string? token)
        {
            client.DefaultRequestHeaders.Authorization =
                token is null ? null : new AuthenticationHeaderValue("Bearer", token);
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
        public async Task Register_ShouldIgnoreRoleField_WhenClientTriesToInjectAdminRole()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            var login = $"escalation-{Guid.NewGuid()}";
            const string password = "P@ssw0rd123";

            // RegisterUserDto has no Role property, so this can't bind — sent as raw JSON to
            // prove the extra field is silently ignored by the model binder, not just unreachable
            // from C# call sites.
            var rawPayload = $$"""{"login":"{{login}}","password":"{{password}}","role":"Admin"}""";
            var registerResponse = await client.PostAsync(
                "/auth/register",
                new StringContent(rawPayload, System.Text.Encoding.UTF8, "application/json"));
            registerResponse.EnsureSuccessStatusCode();

            var token = await LoginAsync(client, login, password);
            SetBearerToken(client, token);

            var response = await client.PostAsJsonAsync("/events", new CreateEventDto
            {
                Title = "Should be forbidden",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                TotalSeats = 10
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_ShouldReturn403_WhenCalledByNonAdminUser()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            var userToken = await RegisterAndLoginAsync(client);
            SetBearerToken(client, userToken);

            var response = await client.PostAsJsonAsync("/events", new CreateEventDto
            {
                Title = "Some event",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                TotalSeats = 10
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Login_ShouldReturn401_WhenPasswordIsIncorrect()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            const string login = "auth-test-user";
            const string password = "correct-password";

            var registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterUserDto
            {
                Login = login,
                Password = password
            });
            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginUserDto
            {
                Login = login,
                Password = "wrong-password"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        }

        [Fact]
        public async Task CancelBooking_ShouldReturn403_WhenCancellingOtherUsersBooking()
        {
            await _fixture.ResetDatabaseAsync();

            await using var factory = new CustomWebApplicationFactory(_fixture.ConnectionString);
            using var client = factory.CreateClient();

            var adminToken = await SeedAdminAndLoginAsync(factory, client);
            SetBearerToken(client, adminToken);

            var createEventResponse = await client.PostAsJsonAsync("/events", new CreateEventDto
            {
                Title = "Shared event",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                TotalSeats = 10
            });
            createEventResponse.EnsureSuccessStatusCode();
            var eventId = await createEventResponse.Content.ReadFromJsonAsync<Guid>();

            var ownerToken = await RegisterAndLoginAsync(client);
            SetBearerToken(client, ownerToken);

            var bookingResponse = await client.PostAsync($"/events/{eventId}/book", content: null);
            bookingResponse.EnsureSuccessStatusCode();
            var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponseDto>();

            var otherUserToken = await RegisterAndLoginAsync(client);
            SetBearerToken(client, otherUserToken);

            var cancelResponse = await client.DeleteAsync($"/bookings/{booking!.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, cancelResponse.StatusCode);
        }
    }
}
