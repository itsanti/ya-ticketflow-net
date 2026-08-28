using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Events.Application.Services;

namespace TicketFlow.Events.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();

            return services;
        }
    }
}
