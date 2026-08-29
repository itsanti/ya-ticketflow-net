using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using TicketFlow.Users.Application.DTOs;

namespace TicketFlow.IntegrationTests.Users
{
    /// <summary>
    /// End-to-end HTTP tests over the real Users pipeline (routing, GlobalExceptionHandlingMiddleware).
    /// </summary>
    [Collection("Users PostgreSql collection")]
    public class AuthHttpTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public AuthHttpTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
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

            var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginUserDto
            {
                Login = login,
                Password = password
            });
            loginResponse.EnsureSuccessStatusCode();

            var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth!.Token);

            Assert.Equal("User", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        }
    }
}
