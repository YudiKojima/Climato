using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Locations;

namespace Climato.Provider.Application.Services;

public sealed class LocationAppService : ILocationAppService
{
    private const string CachePrefix = "locais";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(24);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly IProviderCache cache;

    public LocationAppService(IOpenWeatherClient openWeatherClient, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<Location>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<List<Location>>(CachePrefix, query, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var response = await openWeatherClient.GetLocationsAsync(query, cancellationToken);
        var locations = response.Select(ToLocation).ToList();

        await cache.SetAsync(CachePrefix, query, locations, cacheDuration, cancellationToken);
        return locations;
    }

    private static Location ToLocation(OpenWeatherLocationResponse location)
    {
        return new Location
        {
            Name = location.PortugueseName,
            State = location.State,
            Country = location.Country
        };
    }
}
