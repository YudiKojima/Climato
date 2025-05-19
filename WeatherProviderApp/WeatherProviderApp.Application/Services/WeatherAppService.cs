using Microsoft.Extensions.Configuration;
using WeatherProviderApp.Application.Contracts;
using WeatherProviderApp.Domain.Weathers;

namespace WeatherProviderApp.Application.Services
{
    public class WeatherAppService : IWeatherAppService
    {
        private readonly HttpClient client;
        private readonly string apiKey;
        private readonly ICityImageAppService cityImageAppService;

        public WeatherAppService(HttpClient httpClient, IConfiguration config, ICityImageAppService cityImageAppService)
        {
            client = httpClient;
            apiKey = config["OpenWeather:ApiKey"]!;
            this.cityImageAppService = cityImageAppService;
        }

        public async Task<Weather> GetByCityAsync(string cityName)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={apiKey}&units=metric&lang=pt_br";
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var result = await response.ReadContentAsDictionaryAsync();
            var weather = new Weather
            {
                City = result["name"]?.ToString(),
                Country = ((Dictionary<string, object>)result["sys"])["country"]?.ToString(),
                Description = ((List<object>)result["weather"])[0] is Dictionary<string, object> weatherDict ? weatherDict["description"]?.ToString() : null,
                Icon = ((List<object>)result["weather"])[0] is Dictionary<string, object> weatherIconDict ? weatherIconDict["icon"]?.ToString() : null,
                Temperature = Convert.ToDouble(((Dictionary<string, object>)result["main"])["temp"]),
                FeelsLike = Convert.ToDouble(((Dictionary<string, object>)result["main"])["feels_like"]),
                TempMin = Convert.ToDouble(((Dictionary<string, object>)result["main"])["temp_min"]),
                TempMax = Convert.ToDouble(((Dictionary<string, object>)result["main"])["temp_max"]),
                Humidity = Convert.ToInt32(((Dictionary<string, object>)result["main"])["humidity"]),
                Pressure = Convert.ToInt32(((Dictionary<string, object>)result["main"])["pressure"]),
                WindSpeed = Convert.ToDouble(((Dictionary<string, object>)result["wind"])["speed"]),
                WindDirection = Convert.ToInt32(((Dictionary<string, object>)result["wind"])["deg"]),
                Sunrise = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(((Dictionary<string, object>)result["sys"])["sunrise"])).DateTime,
                Sunset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(((Dictionary<string, object>)result["sys"])["sunset"])).DateTime,
            };

            var cityImageUrl = await cityImageAppService.GetImageUrlAsync(weather.City);
            weather.ImageUrl = cityImageUrl;

            return weather;
        }
    }
}
