using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Bookings.Application.Options;
using TicketFlow.Bookings.Application.Services;
using TicketFlow.Bookings.Application.Services.Background;

namespace TicketFlow.Bookings.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BookingSettings>(configuration.GetSection(BookingSettings.SectionName));

            services.AddScoped<IBookingService, BookingService>();

            services.AddHostedService<BookingProcessingBackgroundService>();

            return services;
        }
    }
}
