using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string Login { get; private set; }

        public string PasswordHash { get; private set; }

        public UserRole Role { get; private set; }

        private User()
        {
            Login = null!;
            PasswordHash = null!;
        }

        public static User Create(string login, string passwordHash, UserRole role)
        {
            if (string.IsNullOrEmpty(login))
                throw new ValidationException("Login must be provided");

            return new User
            {
                Id = Guid.NewGuid(),
                Login = login,
                PasswordHash = passwordHash,
                Role = role
            };
        }
    }
}
