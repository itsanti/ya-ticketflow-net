using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.Domain.Enums;
using TicketFlow.Infrastructure.Security;

namespace TicketFlow.Tests
{
    public class JwtTokenGeneratorTests
    {
        private static JwtOptions CreateOptions() => new()
        {
            Secret = "unit-test-secret-unit-test-secret-32bytes",
            Issuer = "TicketFlow.Tests",
            Audience = "TicketFlow.Tests.Client",
            ExpirationMinutes = 60
        };

        [Fact]
        public void GenerateToken_ShouldIncludeExpectedClaims()
        {
            var options = CreateOptions();
            var generator = new JwtTokenGenerator(Options.Create(options));

            var userId = Guid.NewGuid();
            var token = generator.GenerateToken(userId, "john", UserRole.Admin);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("john", jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
            Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
            Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
            Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub);
        }

        [Fact]
        public void GenerateToken_ShouldSetIssuerAndAudience()
        {
            var options = CreateOptions();
            var generator = new JwtTokenGenerator(Options.Create(options));

            var token = generator.GenerateToken(Guid.NewGuid(), "john", UserRole.User);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(options.Issuer, jwt.Issuer);
            Assert.Equal(options.Audience, jwt.Audiences.Single());
        }

        [Fact]
        public void GenerateToken_ShouldGenerateDistinctJti_ForEachCall()
        {
            var options = CreateOptions();
            var generator = new JwtTokenGenerator(Options.Create(options));

            var handler = new JwtSecurityTokenHandler();
            var token1 = handler.ReadJwtToken(generator.GenerateToken(Guid.NewGuid(), "john", UserRole.User));
            var token2 = handler.ReadJwtToken(generator.GenerateToken(Guid.NewGuid(), "john", UserRole.User));

            var jti1 = token1.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var jti2 = token2.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            Assert.NotEqual(jti1, jti2);
        }

        [Fact]
        public void GenerateToken_ShouldProduceTokenValidatableWithConfiguredSecret()
        {
            var options = CreateOptions();
            var generator = new JwtTokenGenerator(Options.Create(options));

            var userId = Guid.NewGuid();
            var token = generator.GenerateToken(userId, "john", UserRole.Admin);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret))
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

            Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.True(principal.IsInRole(nameof(UserRole.Admin)));
        }

        [Fact]
        public void GenerateToken_ShouldFailValidation_WhenSecretDoesNotMatch()
        {
            var options = CreateOptions();
            var generator = new JwtTokenGenerator(Options.Create(options));

            var token = generator.GenerateToken(Guid.NewGuid(), "john", UserRole.User);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-secret-32-bytes"))
            };

            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
                new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _));
        }
    }
}
