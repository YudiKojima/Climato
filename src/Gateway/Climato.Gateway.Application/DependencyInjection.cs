using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Users;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
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

        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ICityAppService, CityAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IWeatherAppService, WeatherAppService>();
        services.AddScoped<ILocationAppService, LocationAppService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
