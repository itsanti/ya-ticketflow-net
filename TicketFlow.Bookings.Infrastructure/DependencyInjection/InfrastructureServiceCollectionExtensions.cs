using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Bookings.Application.Abstractions;
using TicketFlow.Bookings.Infrastructure.Persistence;
using TicketFlow.Bookings.Infrastructure.Repositories;

namespace TicketFlow.Bookings.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            string? connectionString,
            IConfiguration configuration)
        {
            services.AddDbContext<BookingsDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IBookingRepository, BookingRepository>();

            return services;
        }

        public static void ApplyMigrations(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            dbContext.Database.Migrate();
        }
    }
}
