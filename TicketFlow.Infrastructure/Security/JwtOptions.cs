namespace TicketFlow.Infrastructure.Security
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public required string Secret { get; set; }

        public required string Issuer { get; set; }

        public required string Audience { get; set; }

        public required int ExpirationMinutes { get; set; }
    }
}
