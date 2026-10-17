using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IWeatherProviderClient weatherProviderClient;

    public WeatherAppService(ICityRepository repository, IWeatherProviderClient providerClient)
    {
        cityRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        weatherProviderClient = providerClient ?? throw new ArgumentNullException(nameof(providerClient));
    }

    public async Task<Weather> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken)
    {
        var weather = await weatherProviderClient.GetWeatherAsync(cityName, cancellationToken);
        var city = await cityRepository.GetByNameAsync(weather.City, userId, cancellationToken);

        if (city == null)
        {
            var newCity = new City(weather.City, weather.Country, userId);
            await cityRepository.CreateAsync(newCity, cancellationToken);
        }

        return weather;
    }
}
