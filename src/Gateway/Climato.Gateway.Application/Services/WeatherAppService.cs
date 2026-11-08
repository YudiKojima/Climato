using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly IWeatherProviderClient weatherProviderClient;

    public WeatherAppService(IWeatherProviderClient providerClient)
    {
        weatherProviderClient = providerClient;
    }

    public Task<WeatherDto?> GetByCityAsync(string cityName, bool includeImage, CancellationToken cancellationToken)
    {
        return weatherProviderClient.GetWeatherAsync(cityName, includeImage, cancellationToken);
    }

    public Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        return weatherProviderClient.GetForecastAsync(cityName, cancellationToken);
    }
}
