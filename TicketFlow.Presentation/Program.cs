
using TicketFlow.Application.Abstractions;
using TicketFlow.Application.DependencyInjection;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Infrastructure.DependencyInjection;
using TicketFlow.Presentation.DependencyInjection;
using TicketFlow.Presentation.Middlewares;

namespace TicketFlow.Presentation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInfrastructureServices(connectionString, builder.Configuration);

            builder.Services.AddApplicationServices();

            builder.Services.AddPresentationServices(builder.Configuration);

            var app = builder.Build();

            app.Services.ApplyMigrations();

            // Maintenance command: no HTTP endpoint creates Admins
            //   dotnet run -- create-admin <login> <password>
            if (args.Length > 0 && string.Equals(args[0], "create-admin", StringComparison.OrdinalIgnoreCase))
            {
                await CreateAdminAsync(app.Services, args);
                return;
            }

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseRequestLogging();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

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
