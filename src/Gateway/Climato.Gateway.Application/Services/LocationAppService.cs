using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Locations;

namespace Climato.Gateway.Application.Services;

public class LocationAppService : ILocationAppService
{
    private readonly IWeatherProviderClient weatherProviderClient;

    public LocationAppService(IWeatherProviderClient providerClient)
    {
        weatherProviderClient = providerClient;
    }

    public Task<IReadOnlyList<LocationDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        return weatherProviderClient.SearchLocationsAsync(query.Trim(), cancellationToken);
    }
}
