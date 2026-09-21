using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TicketFlow.Users.Presentation.DependencyInjection
{
    public static class ObservabilityServiceCollectionExtensions
    {
        private const string DefaultServiceName = "users-service";

        public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceName = configuration["Otlp:ServiceName"] ?? DefaultServiceName;

            var otlpEndpoint = configuration["Otlp:Endpoint"];

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation();

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                    }
                })
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter());

            return services;
        }
    }
}
