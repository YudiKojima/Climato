using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Core.Collections;
using Climato.Provider.Application.Options;
using Climato.Provider.Domain.Weathers;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly ICityImageAppService cityImageAppService;

    public WeatherAppService(HttpClient httpClient, IOptions<OpenWeatherOptions> options, ICityImageAppService cityImageAppService)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
        this.cityImageAppService = cityImageAppService;
    }

    public async Task<Weather> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";
        var response = await client.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.ReadContentAsDictionaryAsync(cancellationToken);
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

        weather.ImageUrl = await cityImageAppService.GetImageUrlAsync(weather.City, cancellationToken);

        return weather;
    }
}
