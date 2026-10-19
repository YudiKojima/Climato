using Climato.Provider.Api.Errors;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Climato.Provider.Api;

public static class DependencyInjection
{
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

        return services;
    }
}
