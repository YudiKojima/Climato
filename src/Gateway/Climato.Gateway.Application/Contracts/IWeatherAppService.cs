using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherAppService
{
    Task<WeatherDto?> GetByCityAsync(string cityName, bool includeImage, CancellationToken cancellationToken);
    Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken);
}
