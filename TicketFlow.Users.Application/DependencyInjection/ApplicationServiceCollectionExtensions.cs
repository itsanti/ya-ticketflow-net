using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Users.Application.Services;

namespace TicketFlow.Users.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
