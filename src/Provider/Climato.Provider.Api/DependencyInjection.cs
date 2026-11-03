using Climato.Provider.Api.Errors;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Climato.Provider.Api;

public static class DependencyInjection
{
    private static readonly TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddProviderApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        var healthChecks = services.AddHealthChecks();
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecks.AddRedis(redisConnectionString, name: "redis", failureStatus: HealthStatus.Degraded, tags: ["ready"]);
        }

        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                registration.Timeout = healthCheckTimeout;
            }
        });

        return services;
    }
}
