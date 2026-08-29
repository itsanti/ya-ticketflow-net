using Microsoft.EntityFrameworkCore;
using TicketFlow.Users.Domain.Entities;

namespace TicketFlow.Users.Infrastructure.Persistence
{
    public sealed class UsersDbContext : DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
        }
    }
}
