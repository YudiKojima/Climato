using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Climato.Gateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayApplication(this IServiceCollection services)
    {
        var mappingConfig = TypeAdapterConfig.GlobalSettings;
        mappingConfig.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(mappingConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<ICityAppService, CityAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IWeatherAppService, WeatherAppService>();

        return services;
    }
}
