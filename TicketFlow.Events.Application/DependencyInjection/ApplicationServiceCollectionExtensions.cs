using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Events.Application.Options;
using TicketFlow.Events.Application.Services;

namespace TicketFlow.Events.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

            services.AddScoped<IEventService, EventService>();

            return services;
        }
    }
}
