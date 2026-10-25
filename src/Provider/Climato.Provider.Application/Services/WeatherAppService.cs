using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Services;

public sealed class WeatherAppService : IWeatherAppService
{
    private const string CachePrefix = "clima";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(10);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly ICityImageAppService cityImageAppService;
    private readonly IProviderCache cache;

    public WeatherAppService(IOpenWeatherClient openWeatherClient, ICityImageAppService cityImageAppService, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cityImageAppService = cityImageAppService;
        this.cache = cache;
    }

    public async Task<Weather?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<Weather>(CachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var current = await openWeatherClient.GetCurrentAsync(cityName, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = await cityImageAppService.GetImageUrlAsync(current.Name, cancellationToken);
        var weather = ToWeather(current, imageUrl);

        await cache.SetAsync(CachePrefix, cityName, weather, cacheDuration, cancellationToken);
        return weather;
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
