using Climato.Provider.Api.Errors;

namespace Climato.Provider.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();

        return services;
    }
}
