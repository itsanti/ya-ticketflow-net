using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IUserService, UserService>();

            services.AddHostedService<BookingProcessingBackgroundService>();

            return services;
        }
    }
}
