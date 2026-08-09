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
        public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenUserDoesNotExist()
        {
            _userRepo
                .Setup(r => r.GetByLoginAsync("unknown", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _userService.LoginAsync("unknown", "any-password"));
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
