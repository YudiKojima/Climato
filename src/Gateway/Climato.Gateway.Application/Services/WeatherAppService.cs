using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.Domain.Cities;

namespace Climato.Gateway.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IWeatherProviderClient weatherProviderClient;

    public WeatherAppService(ICityRepository repository, IWeatherProviderClient providerClient)
    {
        cityRepository = repository;
        weatherProviderClient = providerClient;
    }

    public async Task<WeatherDto?> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken)
    {
        var weather = await weatherProviderClient.GetWeatherAsync(cityName, cancellationToken);

        if (weather == null)
        {
            return null;
        }

        var city = await cityRepository.GetByNameAsync(weather.City, userId, cancellationToken);

        if (city == null)
        {
            await cityRepository.CreateAsync(new City(weather.City, weather.Country, userId), cancellationToken);
        }

        return weather;
    }

    public Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        return weatherProviderClient.GetForecastAsync(cityName, cancellationToken);
    }
}
