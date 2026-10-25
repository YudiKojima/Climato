using Climato.Provider.Application.Model.OpenWeather;

namespace Climato.Provider.Application.Contracts;

public interface IOpenWeatherClient
{
    Task<OpenWeatherCurrentResponse?> GetCurrentAsync(string cityName, CancellationToken cancellationToken);
    Task<OpenWeatherForecastResponse?> GetForecastAsync(string cityName, CancellationToken cancellationToken);
}
