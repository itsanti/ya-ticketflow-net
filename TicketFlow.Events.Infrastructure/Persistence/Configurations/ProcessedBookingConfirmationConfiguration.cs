using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Infrastructure.Persistence.Configurations
{
    internal sealed class ProcessedBookingConfirmationConfiguration : IEntityTypeConfiguration<ProcessedBookingConfirmation>
    {
        public void Configure(EntityTypeBuilder<ProcessedBookingConfirmation> builder)
        {
            builder.ToTable("processed_booking_confirmations");

            builder.HasKey(p => p.BookingId);

            builder.Property(p => p.BookingId)
                .HasColumnName("booking_id")
                .ValueGeneratedNever();

            builder.Property(p => p.ProcessedAtUtc)
                .HasColumnName("processed_at_utc")
                .IsRequired();
        }
    }
}
