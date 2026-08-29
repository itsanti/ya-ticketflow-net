using Microsoft.EntityFrameworkCore;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Infrastructure.Persistence
{
    public sealed class EventsDbContext : DbContext
    {
        public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options) { }

        public DbSet<Event> Events => Set<Event>();

        public DbSet<ProcessedBookingConfirmation> ProcessedBookingConfirmations => Set<ProcessedBookingConfirmation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
        }
    }
}
