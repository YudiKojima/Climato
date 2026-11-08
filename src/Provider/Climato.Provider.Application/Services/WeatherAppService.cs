using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Services;

public sealed class WeatherAppService : IWeatherAppService
{
    private const string CachePrefix = "clima";
    private const string CachePrefixWithoutImage = "clima-sem-imagem";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(10);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly ICityResolver cityResolver;
    private readonly ICityImageAppService cityImageAppService;
    private readonly IProviderCache cache;

    public WeatherAppService(
        IOpenWeatherClient openWeatherClient,
        ICityResolver cityResolver,
        ICityImageAppService cityImageAppService,
        IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cityResolver = cityResolver;
        this.cityImageAppService = cityImageAppService;
        this.cache = cache;
    }

    public async Task<Weather?> GetByCityAsync(string cityName, bool includeImage, CancellationToken cancellationToken)
    {
        var cachePrefix = includeImage ? CachePrefix : CachePrefixWithoutImage;
        var cached = await cache.GetAsync<Weather>(cachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var city = await cityResolver.ResolveAsync(cityName, cancellationToken);

        if (city == null)
        {
            return null;
        }

        var current = await openWeatherClient.GetCurrentAsync(city.Lat, city.Lon, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = includeImage
            ? await cityImageAppService.GetImageUrlAsync(city.OriginalName, cancellationToken)
            : null;
        var weather = ToWeather(city, current, imageUrl);

        await cache.SetAsync(cachePrefix, cityName, weather, cacheDuration, cancellationToken);
        return weather;
    }

    private static Weather ToWeather(ResolvedCity city, OpenWeatherCurrentResponse current, string? imageUrl)
    {
        var offset = TimeSpan.FromSeconds(current.Timezone);
        var condition = current.Weather[0];

        return new Weather
        {
            City = city.Name,
            Country = city.Country,
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
