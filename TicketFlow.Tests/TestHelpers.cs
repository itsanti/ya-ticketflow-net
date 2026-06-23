using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.DataAccess;
using TicketFlow.Models;
using TicketFlow.Services;

namespace TicketFlow.Tests
{
    internal static class TestHelpers
    {
        internal static Event CreateTestEvent(int totalSeats)
        {
            return Event.Create(
                "Тестовое событие",
                "Описание тестового события",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                totalSeats
            );
        }

        public static ServiceProvider Create()
        {
            var dbName = Guid.NewGuid().ToString();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();

            return services.BuildServiceProvider();
        }
    }
}
