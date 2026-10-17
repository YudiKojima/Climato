using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherAppService
{
    Task<WeatherDto> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken);
}
