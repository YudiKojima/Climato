using WeatherGatewayApp.Domain.Weathers;

namespace WeatherGatewayApp.Application.Contracts
{
    public interface IWeatherAppService
    {
        Task<Weather> GetByCityAsync(string cityName);
    }
}
