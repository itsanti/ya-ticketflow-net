using TicketFlow.Events.Application.DependencyInjection;
using TicketFlow.Events.Infrastructure.DependencyInjection;
using TicketFlow.Events.Presentation.DependencyInjection;
using TicketFlow.Events.Presentation.Middlewares;

namespace TicketFlow.Events.Presentation
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

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

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
    }
}
