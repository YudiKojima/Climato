using WeatherGatewayApp.Application.Contracts;
using WeatherGatewayApp.Domain.Cities;
using WeatherGatewayApp.Domain.Weathers;

namespace WeatherGatewayApp.Application.Services
{
    public class WeatherAppService : IWeatherAppService
    {
        private readonly ICityRepository cityRepository;
        private readonly HttpClient client;

        public WeatherAppService(ICityRepository repository, HttpClient httpClient)
        {
            cityRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            client = httpClient;
        }

        public async Task<Weather> GetByCityAsync(string cityName, Guid userId)
        {
            var url = $"https://localhost:44313/weathers?cityName={cityName}";
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var result = await response.ReadContentAsDictionaryAsync();

            var weather = new Weather
            {
                City = result["city"]?.ToString(),
                Country = result["country"]?.ToString(),
                Description = result["description"]?.ToString(),
                Icon = result["icon"]?.ToString(),
                Temperature = Convert.ToDouble(result["temperature"]),
                FeelsLike = Convert.ToDouble(result["feelsLike"]),
                TempMin = Convert.ToDouble(result["tempMin"]),
                TempMax = Convert.ToDouble(result["tempMax"]),
                Humidity = Convert.ToInt32(result["humidity"]),
                Pressure = Convert.ToInt32(result["pressure"]),
                WindSpeed = Convert.ToDouble(result["windSpeed"]),
                WindDirection = Convert.ToInt32(result["windDirection"]),
                Sunrise = DateTime.Parse(result["sunrise"]?.ToString() ?? DateTime.MinValue.ToString()),
                Sunset = DateTime.Parse(result["sunset"]?.ToString() ?? DateTime.MinValue.ToString()),
                ImageUrl = result["imageUrl"]?.ToString()
            };

            var city = await cityRepository.GetByNameAsync(weather.City, userId);

            if (city == null)
            {
                var newCity = new City(weather.City, weather.Country, userId);
                await cityRepository.CreateAsync(newCity);
            }

            return weather;
        }
    }
}
