using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Presentation;

namespace TicketFlow.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Boots the real ASP.NET Core pipeline (routing, auth, middlewares) against the
    /// Testcontainers Postgres instance owned by <see cref="PostgreSqlTestFixture"/>.
    /// Runs under the Development environment so Program.cs's eager config reads
    /// (Jwt options bound before Build()) resolve against appsettings.Development.json —
    /// only the DB connection is swapped, to the disposable Testcontainers instance instead
    /// of the local docker-compose Postgres. Overriding Jwt:Secret via ConfigureAppConfiguration
    /// would be unreliable here: AddAuthenticationServices reads it eagerly inside Program.Main,
    /// before WebApplicationFactory's customizations are attached.
    /// </summary>
    public class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
