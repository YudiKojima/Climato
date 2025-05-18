using WeatherProviderApp.Domain.Weathers;

namespace WeatherProviderApp.Application.Contracts
{
    public interface IWeatherAppService
    {
        Task<Weather> GetByCityAsync(string cityName);
    }
}
