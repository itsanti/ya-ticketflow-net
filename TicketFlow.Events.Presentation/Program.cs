using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
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

            builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(new CompactJsonFormatter()));

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddInfrastructureServices(connectionString, builder.Configuration);

            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddPresentationServices(builder.Configuration);

            builder.Services.AddObservability(builder.Configuration);

            var app = builder.Build();

            app.Services.ApplyMigrations();

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            app.UseSerilogRequestLogging(options =>
            {
                // Скрейп Prometheus идёт раз в 15 секунд и иначе забивает лог.
                options.GetLevel = (context, _, exception) => exception is not null || context.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : context.Request.Path.StartsWithSegments("/metrics")
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information;
            });

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseWhen(
                context => !context.Request.Path.StartsWithSegments("/metrics"),
                branch => branch.UseHttpsRedirection());

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapPrometheusScrapingEndpoint();

            await app.RunAsync();
        }
    }
}
