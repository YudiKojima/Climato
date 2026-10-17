using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Infrastructure.Http;

public class WeatherProviderClient : IWeatherProviderClient
{
    private readonly HttpClient client;

    public WeatherProviderClient(HttpClient httpClient)
    {
        client = httpClient;
    }

    public async Task<Weather> GetWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"weathers?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.ReadContentAsDictionaryAsync(cancellationToken);

        return new Weather
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
    }
}
