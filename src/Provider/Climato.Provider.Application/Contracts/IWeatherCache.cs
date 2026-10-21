using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts;

public interface IWeatherCache
{
    Task<Weather?> GetAsync(string cityName, CancellationToken cancellationToken);
    Task SetAsync(string cityName, Weather weather, CancellationToken cancellationToken);
}
