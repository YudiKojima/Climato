using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Services;

public sealed class ForecastAppService : IForecastAppService
{
    private const string CachePrefix = "previsao";
    private const int MaxDays = 5;

    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan midday = TimeSpan.FromHours(12);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly IProviderCache cache;

    public ForecastAppService(IOpenWeatherClient openWeatherClient, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cache = cache;
    }

    public async Task<Forecast?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<Forecast>(CachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var response = await openWeatherClient.GetForecastAsync(cityName, cancellationToken);

        if (response == null)
        {
            return null;
        }

        var forecast = ToForecast(response);

        await cache.SetAsync(CachePrefix, cityName, forecast, cacheDuration, cancellationToken);
        return forecast;
    }

    private static Forecast ToForecast(OpenWeatherForecastResponse response)
    {
        var offset = TimeSpan.FromSeconds(response.City.Timezone);

        var days = response.List
            .Select(item => (Item: item, Local: DateTimeOffset.FromUnixTimeSeconds(item.Dt).ToOffset(offset)))
            .GroupBy(entry => DateOnly.FromDateTime(entry.Local.DateTime))
            .OrderBy(group => group.Key)
            .Take(MaxDays)
            .Select(group => ToForecastDay(group.Key, group.ToList()))
            .ToList();

        return new Forecast
        {
            City = response.City.Name,
            Country = response.City.Country,
            Days = days
        };
    }

    private static ForecastDay ToForecastDay(DateOnly date, IReadOnlyList<(OpenWeatherForecastItem Item, DateTimeOffset Local)> entries)
    {
        var middayItem = entries.MinBy(entry => (entry.Local.TimeOfDay - midday).Duration()).Item;
        var condition = middayItem.Weather[0];

        return new ForecastDay
        {
            Date = date,
            Description = condition.Description,
            Icon = condition.Icon,
            TempMin = entries.Min(entry => entry.Item.Main.TempMin),
            TempMax = entries.Max(entry => entry.Item.Main.TempMax),
            PrecipitationProbability = (int)Math.Round(entries.Max(entry => entry.Item.Pop) * 100),
            WindSpeedKmh = Math.Round(entries.Max(entry => entry.Item.Wind.Speed) * 3.6, 1)
        };
    }
}
