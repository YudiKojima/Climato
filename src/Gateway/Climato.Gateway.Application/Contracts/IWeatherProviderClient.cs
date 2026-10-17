using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherProviderClient
{
    Task<Weather> GetWeatherAsync(string cityName, CancellationToken cancellationToken);
}
