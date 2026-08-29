using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Users.Infrastructure.Persistence;

namespace TicketFlow.IntegrationTests.Users
{
    /// <summary>
    /// Development-окружение нужно, чтобы Program.cs читал appsettings.Development.json
    /// (там же лежит общий тестовый/дев Jwt:Secret) — подмена конфигурации через
    /// ConfigureAppConfiguration ненадёжна, т.к. чтение конфигурации происходит раньше.
    /// </summary>
    public class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<TicketFlow.Users.Presentation.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UsersDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
