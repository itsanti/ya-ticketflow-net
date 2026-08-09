using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Application.Concurrency;
using TicketFlow.Application.Options;
using TicketFlow.Application.Services;
using TicketFlow.Application.Services.Background;

namespace TicketFlow.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BookingSettings>(configuration.GetSection(BookingSettings.SectionName));

            // Singleton: the per-event lock must be shared across scoped BookingService
            // instances (one per request) to actually serialize concurrent bookings.
            services.AddSingleton<KeyedAsyncLock>();

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IUserService, UserService>();

            services.AddHostedService<BookingProcessingBackgroundService>();

            return services;
        }
    }
}
