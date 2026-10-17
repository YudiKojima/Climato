using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherAppService
{
    Task<Weather> GetByCityAsync(string cityName, Guid userId);
}
