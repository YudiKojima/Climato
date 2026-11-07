using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;

namespace Climato.Provider.Application.Services;

public sealed class CityResolver : ICityResolver
{
    private const string CachePrefix = "cidades";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(24);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly IProviderCache cache;

    public CityResolver(IOpenWeatherClient openWeatherClient, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cache = cache;
    }

    public async Task<ResolvedCity?> ResolveAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<ResolvedCity>(CachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var locations = await openWeatherClient.GetLocationsAsync(cityName, cancellationToken);
        var first = locations.FirstOrDefault();

        if (first == null)
        {
            return null;
        }

        var city = new ResolvedCity(first.PortugueseName, first.Name, first.Country, first.Lat, first.Lon);

        await cache.SetAsync(CachePrefix, cityName, city, cacheDuration, cancellationToken);
        return city;
    }
}
