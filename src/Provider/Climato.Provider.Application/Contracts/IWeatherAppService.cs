using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts
{
    public interface IWeatherAppService
    {
        Task<Weather> GetByCityAsync(string cityName);
    }
}
