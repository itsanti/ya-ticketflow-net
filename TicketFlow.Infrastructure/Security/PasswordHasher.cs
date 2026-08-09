using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TicketFlow.Application.Abstractions;

namespace TicketFlow.Infrastructure.Security
{
    /// <summary>
    /// Hashes passwords with BCrypt. Retains the ability to verify (but never produce)
    /// legacy 64-char hex SHA-256 hashes created before this migration, so existing
    /// users are not locked out. New hashes are always BCrypt.
    /// </summary>
    public partial class PasswordHasher : IPasswordHasher
    {
        private const int BCryptWorkFactor = 12;

        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: BCryptWorkFactor);
        }

        public bool Verify(string password, string passwordHash)
        {
            if (IsLegacySha256Hash(passwordHash))
            {
                return VerifyLegacySha256(password, passwordHash);
            }

            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        private static bool IsLegacySha256Hash(string passwordHash)
        {
            return LegacyHashRegex().IsMatch(passwordHash);
        }

        private static bool VerifyLegacySha256(string password, string passwordHash)
        {
            var computed = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var expected = Convert.FromHexString(passwordHash);

            return CryptographicOperations.FixedTimeEquals(computed, expected);
        }

        [GeneratedRegex("^[0-9A-Fa-f]{64}$")]
        private static partial Regex LegacyHashRegex();
    }
}
