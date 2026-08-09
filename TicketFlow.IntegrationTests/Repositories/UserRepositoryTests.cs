using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Infrastructure.Repositories;
using TicketFlow.IntegrationTests.Infrastructure;

namespace TicketFlow.IntegrationTests.Repositories
{
    [Collection("PostgreSql collection")]
    public class UserRepositoryTests
    {
        private readonly PostgreSqlTestFixture _fixture;

        public UserRepositoryTests(PostgreSqlTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static User CreateUser(string login) =>
            User.Create(login, "hash", UserRole.User);

        [Fact]
        public async Task AddAsync_ShouldPersistUser()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new UserRepository(context);

            var user = CreateUser($"john-{Guid.NewGuid()}");

            await repository.AddAsync(user);
            await repository.SaveChangesAsync();

            var storedUser = await context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            Assert.NotNull(storedUser);
            Assert.Equal(user.Login, storedUser.Login);
            Assert.Equal("hash", storedUser.PasswordHash);
            Assert.Equal(UserRole.User, storedUser.Role);
        }

        [Fact]
        public async Task GetByLoginAsync_ShouldReturnUser_WhenUserExists()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new UserRepository(context);

            var login = $"john-{Guid.NewGuid()}";
            var user = CreateUser(login);

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var storedUser = await repository.GetByLoginAsync(login);

            Assert.NotNull(storedUser);
            Assert.Equal(user.Id, storedUser.Id);
        }

        [Fact]
        public async Task GetByLoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            await _fixture.ResetDatabaseAsync();

            await using var context = _fixture.CreateContext();
            var repository = new UserRepository(context);

            var storedUser = await repository.GetByLoginAsync($"unknown-{Guid.NewGuid()}");

            Assert.Null(storedUser);
        }

        [Fact]
        public async Task AddAsync_ShouldThrowUniqueConstraintViolation_WhenLoginAlreadyExists()
        {
            await _fixture.ResetDatabaseAsync();

            var login = $"john-{Guid.NewGuid()}";

            await using (var seedContext = _fixture.CreateContext())
            {
                await seedContext.Users.AddAsync(CreateUser(login));
                await seedContext.SaveChangesAsync();
            }

            await using var context = _fixture.CreateContext();
            var repository = new UserRepository(context);

            await repository.AddAsync(CreateUser(login));

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                repository.SaveChangesAsync());

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal("23505", postgresException.SqlState);
        }
    }
}
