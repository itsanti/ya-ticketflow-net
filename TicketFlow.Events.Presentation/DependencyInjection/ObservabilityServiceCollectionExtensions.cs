using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketFlow.Events.Presentation.Options;

namespace TicketFlow.Events.Presentation.DependencyInjection
{
    public static class ObservabilityServiceCollectionExtensions
    {
        public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
        {
            var otlpOptions = configuration.GetSection(OtlpOptions.SectionName).Get<OtlpOptions>() ?? new OtlpOptions();

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(otlpOptions.ServiceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation();

                    if (!string.IsNullOrWhiteSpace(otlpOptions.Endpoint))
                    {
                        tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpOptions.Endpoint));
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
