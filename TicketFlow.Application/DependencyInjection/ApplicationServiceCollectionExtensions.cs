using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Services;
using TicketFlow.Application.Services.Background;

namespace TicketFlow.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();

            services.AddHostedService<BookingProcessingBackgroundService>();

            return services;
        }
    }
}
