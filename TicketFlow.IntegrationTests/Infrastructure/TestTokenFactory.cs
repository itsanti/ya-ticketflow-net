using Microsoft.Extensions.Options;
using TicketFlow.Users.Domain.Enums;
using TicketFlow.Users.Infrastructure.Security;

namespace TicketFlow.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Мints tokens locally instead of hitting a running Users service over HTTP: JwtTokenGenerator
    /// is a pure component, а Events/Bookings проверяют тот же secret/issuer/audience из своих
    /// appsettings.Development.json — так что заводить отдельный WebApplicationFactory для Users
    /// внутри тестов Events/Bookings не нужно.
    /// </summary>
    internal static class TestTokenFactory
    {
        private static readonly JwtOptions DevJwtOptions = new()
        {
            Secret = "037048d666b378eb0147eec79ccd91b8e016e23fb5d9f5a131855bf94ef3fb8d",
            Issuer = "TicketFlow",
            Audience = "TicketFlowClient",
            ExpirationMinutes = 60
        };

        public static string CreateToken(Guid userId, string login, UserRole role)
        {
            var generator = new JwtTokenGenerator(Options.Create(DevJwtOptions));
            return generator.GenerateToken(userId, login, role);
        }
    }
}
