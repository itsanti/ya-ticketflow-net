using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Events.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Events
{
    /// <summary>
    /// Поднимает реальный хост Events, включая KafkaTopicInitializer и BookingConfirmedConsumer —
    /// без брокера рядом они просто логируют ошибки подключения в фоне и не мешают HTTP-тестам
    /// (оба спроектированы не ронять приложение при недоступной Kafka).
    /// </summary>
    public class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<TicketFlow.Events.Presentation.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventsDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<EventsDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
