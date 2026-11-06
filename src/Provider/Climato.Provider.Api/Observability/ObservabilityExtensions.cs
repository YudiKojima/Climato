using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace Climato.Provider.Api.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "Climato.Provider";
    private const string DevelopmentTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", ServiceName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName);

            if (environment.IsDevelopment())
            {
                logger.WriteTo.Console(outputTemplate: DevelopmentTemplate);
            }
            else
            {
                logger.WriteTo.Console(new CompactJsonFormatter());
            }
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    tracing.AddOtlpExporter();
                }
            });

        return builder;
    }
}
