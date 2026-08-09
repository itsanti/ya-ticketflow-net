using Moq;
using TicketFlow.Application.Abstractions;
using TicketFlow.Application.Services;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<IJwtTokenGenerator> _jwtGenerator = new();
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userService = new UserService(_userRepo.Object, _hasher.Object, _jwtGenerator.Object);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowValidationException_WhenLoginAlreadyExists()
        {
            var existingUser = User.Create("john", "existing-hash", UserRole.User);

            _userRepo
                .Setup(r => r.GetByLoginAsync("john", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            await Assert.ThrowsAsync<ValidationException>(() =>
                _userService.RegisterAsync("john", "password"));

            _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldHashPasswordAndPersistUserAsUserRole_WhenDataIsValid()
        {
            _userRepo
                .Setup(r => r.GetByLoginAsync("john", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _hasher
                .Setup(h => h.Hash("plain-password"))
                .Returns("hashed-password");

            User? addedUser = null;

            _userRepo
                .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((user, _) => addedUser = user)
                .Returns(Task.CompletedTask);

            await _userService.RegisterAsync("john", "plain-password");

            Assert.NotNull(addedUser);
            Assert.Equal("john", addedUser.Login);
            Assert.Equal("hashed-password", addedUser.PasswordHash);
            Assert.Equal(UserRole.User, addedUser.Role);

            _userRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenUserDoesNotExist()
        {
            _userRepo
                .Setup(r => r.GetByLoginAsync("unknown", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _userService.LoginAsync("unknown", "any-password"));

            _hasher.Verify(
                h => h.Verify("any-password", It.Is<string>(hash => hash != null)),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrect()
        {
            var user = User.Create("john", "stored-hash", UserRole.User);

            _userRepo
                .Setup(r => r.GetByLoginAsync("john", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _hasher
                .Setup(h => h.Verify("wrong-password", "stored-hash"))
                .Returns(false);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _userService.LoginAsync("john", "wrong-password"));
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            var user = User.Create("john", "stored-hash", UserRole.User);

            _userRepo
                .Setup(r => r.GetByLoginAsync("john", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _hasher
                .Setup(h => h.Verify("correct-password", "stored-hash"))
                .Returns(true);

            _jwtGenerator
                .Setup(j => j.GenerateToken(user.Id, user.Login, user.Role))
                .Returns("test-token");

            var token = await _userService.LoginAsync("john", "correct-password");

            Assert.Equal("test-token", token);
        }
    }
}
