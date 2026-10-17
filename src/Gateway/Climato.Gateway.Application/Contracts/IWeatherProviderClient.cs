using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherProviderClient
{
    Task<WeatherDto> GetWeatherAsync(string cityName, CancellationToken cancellationToken);
}
