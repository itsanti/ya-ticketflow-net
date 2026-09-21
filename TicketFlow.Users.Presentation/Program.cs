using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using TicketFlow.Users.Application.Abstractions;
using TicketFlow.Users.Application.DependencyInjection;
using TicketFlow.Users.Domain.Entities;
using TicketFlow.Users.Domain.Enums;
using TicketFlow.Users.Infrastructure.DependencyInjection;
using TicketFlow.Users.Presentation.DependencyInjection;
using TicketFlow.Users.Presentation.Middlewares;

namespace TicketFlow.Users.Presentation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(new CompactJsonFormatter()));

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInfrastructureServices(connectionString, builder.Configuration);

            builder.Services.AddApplicationServices();

            builder.Services.AddPresentationServices(builder.Configuration);

            builder.Services.AddObservability(builder.Configuration);

            var app = builder.Build();

            app.Services.ApplyMigrations();

            // Maintenance command: no HTTP endpoint creates Admins
            //   dotnet run -- create-admin <login> <password>
            if (args.Length > 0 && string.Equals(args[0], "create-admin", StringComparison.OrdinalIgnoreCase))
            {
                await CreateAdminAsync(app.Services, args);
                return;
            }

            app.UseSerilogRequestLogging(options =>
            {
                // Скрейп Prometheus идёт раз в 15 секунд и иначе забивает лог.
                options.GetLevel = (context, _, exception) => exception is not null || context.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : context.Request.Path.StartsWithSegments("/metrics")
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information;
            });

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseWhen(
                context => !context.Request.Path.StartsWithSegments("/metrics"),
                branch => branch.UseHttpsRedirection());

            app.MapControllers();
            app.MapPrometheusScrapingEndpoint();

            await app.RunAsync();
        }

        private static async Task CreateAdminAsync(IServiceProvider services, string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: dotnet run -- create-admin <login> <password>");
                Environment.ExitCode = 1;
                return;
            }

            var login = args[1];
            var password = args[2];

            using var scope = services.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var existingUser = await userRepo.GetByLoginAsync(login);

            if (existingUser != null)
            {
                Console.Error.WriteLine($"User '{login}' already exists.");
                Environment.ExitCode = 1;
                return;
            }

            var admin = User.Create(login, hasher.Hash(password), UserRole.Admin);

            await userRepo.AddAsync(admin);
            await userRepo.SaveChangesAsync();

            Console.WriteLine($"Admin user '{login}' created.");
        }
    }
}
