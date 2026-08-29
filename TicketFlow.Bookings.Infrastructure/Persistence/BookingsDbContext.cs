using Microsoft.EntityFrameworkCore;
using TicketFlow.Bookings.Domain.Entities;

namespace TicketFlow.Bookings.Infrastructure.Persistence
{
    public sealed class BookingsDbContext : DbContext
    {
        public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }

        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
        }
    }
}
