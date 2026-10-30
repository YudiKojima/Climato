using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Services;

namespace Climato.Gateway.UnitTests.Application.Services;

public class LocationAppServiceTests
{
    [Fact]
    public async Task SearchAsync_QueryComEspacos_RepassaAoProviderAparada()
    {
        var providerClient = Substitute.For<IWeatherProviderClient>();
        IReadOnlyList<LocationDto> expected = [new LocationDto { Name = "Lima", Country = "PE" }];
        providerClient.SearchLocationsAsync("Lima", Arg.Any<CancellationToken>()).Returns(expected);
        var appService = new LocationAppService(providerClient);

        var locations = await appService.SearchAsync("  Lima ", TestContext.Current.CancellationToken);

        locations.ShouldBeSameAs(expected);
    }
}
