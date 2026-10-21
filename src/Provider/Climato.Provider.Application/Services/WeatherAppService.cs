using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Exceptions;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Options;
using Climato.Provider.Domain.Weathers;
using Microsoft.Extensions.Options;
using Polly;

namespace Climato.Provider.Application.Services;

public sealed class WeatherAppService : IWeatherAppService
{
    private const string ServiceName = "OpenWeather";

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly ICityImageAppService cityImageAppService;
    private readonly IWeatherCache weatherCache;

    public WeatherAppService(HttpClient httpClient, IOptions<OpenWeatherOptions> options, ICityImageAppService cityImageAppService, IWeatherCache weatherCache)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
        this.cityImageAppService = cityImageAppService;
        this.weatherCache = weatherCache;
    }

    public async Task<Weather?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await weatherCache.GetAsync(cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var current = await GetCurrentAsync(cityName, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = await cityImageAppService.GetImageUrlAsync(current.Name, cancellationToken);
        var weather = ToWeather(current, imageUrl);

        await weatherCache.SetAsync(cityName, weather, cancellationToken);
        return weather;
    }

    private async Task<OpenWeatherCurrentResponse?> GetCurrentAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceUnavailableException(ServiceName, $"status {(int)response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<OpenWeatherCurrentResponse>(jsonOptions, cancellationToken)
                ?? throw new ExternalServiceUnavailableException(ServiceName, "resposta vazia");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ExternalServiceUnavailableException(ServiceName, "falha na chamada", exception);
        }
    }

    private static Weather ToWeather(OpenWeatherCurrentResponse current, string? imageUrl)
    {
        var offset = TimeSpan.FromSeconds(current.Timezone);
        var condition = current.Weather[0];

        return new Weather
        {
            City = current.Name,
            Country = current.Sys.Country,
            Description = condition.Description,
            Icon = condition.Icon,
            Temperature = current.Main.Temp,
            FeelsLike = current.Main.FeelsLike,
            TempMin = current.Main.TempMin,
            TempMax = current.Main.TempMax,
            Humidity = current.Main.Humidity,
            Pressure = current.Main.Pressure,
            WindSpeedKmh = Math.Round(current.Wind.Speed * 3.6, 1),
            WindDirection = current.Wind.Deg,
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(current.Sys.Sunrise).ToOffset(offset),
            Sunset = DateTimeOffset.FromUnixTimeSeconds(current.Sys.Sunset).ToOffset(offset),
            ImageUrl = imageUrl
        };
    }
}
