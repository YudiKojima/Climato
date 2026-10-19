using System.Globalization;
using System.Text;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Weathers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Climato.Provider.Application.Services;

public sealed class WeatherCache : IWeatherCache
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly DistributedCacheEntryOptions entryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly IDistributedCache cache;
    private readonly ILogger<WeatherCache> logger;

    public WeatherCache(IDistributedCache cache, ILogger<WeatherCache> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<Weather?> GetAsync(string cityName, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await cache.GetAsync(CreateKey(cityName), cancellationToken);
            return bytes == null ? null : JsonSerializer.Deserialize<Weather>(bytes, jsonOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao ler o cache de clima de {CityName}; seguindo sem cache.", cityName);
            return null;
        }
    }

    public async Task SetAsync(string cityName, Weather weather, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(weather, jsonOptions);
            await cache.SetAsync(CreateKey(cityName), bytes, entryOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao gravar o cache de clima de {CityName}; seguindo sem cache.", cityName);
        }
    }

    private static string CreateKey(string cityName)
    {
        var words = cityName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var decomposed = string.Join(' ', words).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return $"clima:{builder.ToString().Normalize(NormalizationForm.FormC)}";
    }
}
