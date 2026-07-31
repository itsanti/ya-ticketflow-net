
using TicketFlow.Application.DependencyInjection;
using TicketFlow.Presentation.DependencyInjection;
using TicketFlow.Infrastructure.DependencyInjection;
using TicketFlow.Presentation.Middlewares;

namespace TicketFlow.Presentation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInfrastructureServices(connectionString);

            builder.Services.AddApplicationServices();

            builder.Services.AddPresentationServices();

            var app = builder.Build();

            app.Services.ApplyMigrations();

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
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
