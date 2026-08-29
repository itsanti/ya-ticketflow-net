using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Bookings.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Bookings
{
    /// <summary>
    /// Поднимает реальный хост Bookings, включая BookingProcessingBackgroundService и
    /// singleton IBookingConfirmedPublisher (Kafka). Без брокера рядом публикация просто
    /// падает и логируется — это не мешает HTTP-тестам, т.к. статус брони уже сохранён
    /// в БД до попытки публикации (см. BookingProcessingBackgroundService).
    /// </summary>
    public class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<TicketFlow.Bookings.Presentation.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BookingsDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<BookingsDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
