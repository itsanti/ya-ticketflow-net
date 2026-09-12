using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Infrastructure.Caching;
using TicketFlow.Events.Infrastructure.Messaging;
using TicketFlow.Events.Infrastructure.Persistence;
using TicketFlow.Events.Infrastructure.Repositories;

namespace TicketFlow.Events.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            string? connectionString,
            IConfiguration configuration)
        {
            services.AddDbContext<EventsDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IEventRepository, EventRepository>();

            services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
            services.AddHostedService<KafkaTopicInitializer>();
            services.AddHostedService<BookingConfirmedConsumer>();

            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
                var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
                configurationOptions.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }

        public static void ApplyMigrations(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            dbContext.Database.Migrate();
        }
    }
}
