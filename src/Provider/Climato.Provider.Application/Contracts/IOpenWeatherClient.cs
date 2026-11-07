using Climato.Provider.Application.Model.OpenWeather;

namespace Climato.Provider.Application.Contracts;

public interface IOpenWeatherClient
{
    Task<OpenWeatherCurrentResponse?> GetCurrentAsync(double lat, double lon, CancellationToken cancellationToken);
    Task<OpenWeatherForecastResponse?> GetForecastAsync(double lat, double lon, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpenWeatherLocationResponse>> GetLocationsAsync(string query, CancellationToken cancellationToken);
}
