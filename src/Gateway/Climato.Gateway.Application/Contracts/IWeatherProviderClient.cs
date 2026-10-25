using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherProviderClient
{
    Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken);
    Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(string query, CancellationToken cancellationToken);
}
