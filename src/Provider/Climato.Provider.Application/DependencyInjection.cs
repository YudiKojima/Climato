using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Options;
using Climato.Provider.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderApplication(this IServiceCollection services)
    {
        services.AddOptions<OpenWeatherOptions>()
            .BindConfiguration(OpenWeatherOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PexelsOptions>()
            .BindConfiguration(PexelsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IWeatherAppService, WeatherAppService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddHttpClient<ICityImageAppService, CityImageAppService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PexelsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }
}
