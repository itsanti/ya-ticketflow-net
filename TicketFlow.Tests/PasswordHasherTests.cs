using System.Security.Cryptography;
using System.Text;
using TicketFlow.Infrastructure.Security;

namespace TicketFlow.Tests
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Hash_ShouldReturnBCryptFormattedHash()
        {
            var hash = _hasher.Hash("my-secret-password");

            Assert.StartsWith("$2", hash);
        }

        [Fact]
        public void Hash_ShouldReturnDifferentHashes_ForSamePassword()
        {
            var hash1 = _hasher.Hash("my-secret-password");
            var hash2 = _hasher.Hash("my-secret-password");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Verify_ShouldReturnTrue_WhenPasswordMatchesBCryptHash()
        {
            var hash = _hasher.Hash("correct-password");

            Assert.True(_hasher.Verify("correct-password", hash));
        }

        [Fact]
        public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchBCryptHash()
        {
            var hash = _hasher.Hash("correct-password");

            Assert.False(_hasher.Verify("wrong-password", hash));
        }

        [Fact]
        public void Verify_ShouldReturnTrue_WhenPasswordMatchesLegacySha256Hash()
        {
            var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("legacy-password")));

            Assert.True(_hasher.Verify("legacy-password", legacyHash));
        }

        [Fact]
        public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchLegacySha256Hash()
        {
            var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("legacy-password")));

            Assert.False(_hasher.Verify("wrong-password", legacyHash));
        }
    }
}
