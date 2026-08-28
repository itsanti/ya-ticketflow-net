using TicketFlow.Bookings.Application.DependencyInjection;
using TicketFlow.Bookings.Infrastructure.DependencyInjection;
using TicketFlow.Bookings.Presentation.DependencyInjection;
using TicketFlow.Bookings.Presentation.Middlewares;

namespace TicketFlow.Bookings.Presentation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInfrastructureServices(connectionString, builder.Configuration);

            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddPresentationServices(builder.Configuration);

            var app = builder.Build();

            app.Services.ApplyMigrations();

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseRequestLogging();
            }

            app.UseHttpsRedirection();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
