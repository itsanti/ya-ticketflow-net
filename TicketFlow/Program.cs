
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.DataAccess;
using TicketFlow.Middlewares;
using TicketFlow.Models;
using TicketFlow.Models.Store;
using TicketFlow.Services;
using TicketFlow.Services.Background;

namespace TicketFlow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Add services to the container.
            builder.Services.AddSingleton<IInMemoryStore<Event>, InMemoryEventStore>();
            builder.Services.AddScoped<IEventService, EventService>();

            builder.Services.AddSingleton<IInMemoryStore<Booking>, InMemoryBookingStore>();
            builder.Services.AddScoped<IBookingService, BookingService>();

            builder.Services.AddHostedService<BookingProcessingBackgroundService>();

            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage);
                        var detailMessage = string.Join(" ", errors);
                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Validation error",
                            Detail = detailMessage
                        };

                        return new BadRequestObjectResult(problemDetails);
                    };
                });

            builder.Services.AddProblemDetails();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

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
