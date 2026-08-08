using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Persistence.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(u => u.Login)
                .HasColumnName("login")
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(u => u.Login)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            builder.Property(u => u.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
