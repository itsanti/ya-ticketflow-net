using TicketFlow.Application.Abstractions;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Application.Services
{
    public class UserService(
            IUserRepository userRepo,
            IPasswordHasher hasher,
            IJwtTokenGenerator jwtGenerator
    ) : IUserService
    {
        private const string InvalidCredentialsMessage = "Invalid login or password.";

        private readonly IUserRepository _userRepo = userRepo;
        private readonly IPasswordHasher _hasher = hasher;
        private readonly IJwtTokenGenerator _jwtGenerator = jwtGenerator;

        public async Task RegisterAsync(string login, string password, string role)
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole) || !Enum.IsDefined(parsedRole))
            {
                throw new ValidationException($"Role must be one of: {string.Join(", ", Enum.GetNames<UserRole>())}");
            }

            var existingUser = await _userRepo.GetByLoginAsync(login);

            if (existingUser != null)
            {
                throw new ValidationException("User with this login already exists.");
            }

            var passwordHash = _hasher.Hash(password);
            var user = User.Create(login, passwordHash, parsedRole);

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task<string> LoginAsync(string login, string password)
        {
            var user = await _userRepo.GetByLoginAsync(login);

            if (user == null || !_hasher.Verify(password, user.PasswordHash))
            {
                throw new NotFoundException(InvalidCredentialsMessage);
            }

            return _jwtGenerator.GenerateToken(user.Id, user.Login, user.Role);
        }
    }
}
