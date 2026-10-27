using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Locations;

namespace Climato.Provider.UnitTests.Application.Services;

public class LocationAppServiceTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly LocationAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public LocationAppServiceTests()
    {
        appService = new LocationAppService(openWeatherClient, cache);
    }

    [Fact]
    public async Task SearchAsync_ComNomeEmPortugues_UsaLocalNamesPt()
    {
        ReturnsLocations("Londres", new OpenWeatherLocationResponse("London", new Dictionary<string, string> { ["pt"] = "Londres", ["en"] = "London" }, "GB", "England"));

        var locations = await appService.SearchAsync("Londres", cancellationToken);

        var location = locations.ShouldHaveSingleItem();
        location.Name.ShouldBe("Londres");
        location.State.ShouldBe("England");
        location.Country.ShouldBe("GB");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchAsync_SemNomeEmPortugues_UsaName(bool hasLocalNames)
    {
        var localNames = hasLocalNames ? new Dictionary<string, string> { ["en"] = "Lima" } : null;
        ReturnsLocations("Lima", new OpenWeatherLocationResponse("Lima", localNames, "PE", null));

        var locations = await appService.SearchAsync("Lima", cancellationToken);

        locations.ShouldHaveSingleItem().Name.ShouldBe("Lima");
    }

    [Fact]
    public async Task SearchAsync_SemResultado_GravaListaVaziaNoCachePorVinteEQuatroHoras()
    {
        ReturnsLocations("Xyzw");

        var locations = await appService.SearchAsync("Xyzw", cancellationToken);

        locations.ShouldBeEmpty();
        await cache.Received(1).SetAsync("locais", "Xyzw", Arg.Is<List<Location>>(list => list.Count == 0), TimeSpan.FromHours(24), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_BuscaNoCache_NaoChamaAOpenWeather()
    {
        cache.GetAsync<List<Location>>("locais", "Lima", Arg.Any<CancellationToken>()).Returns([new Location { Name = "Lima", Country = "PE" }]);

        var locations = await appService.SearchAsync("Lima", cancellationToken);

        locations.ShouldHaveSingleItem().Name.ShouldBe("Lima");
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetLocationsAsync(default!, cancellationToken);
    }

    private void ReturnsLocations(string query, params OpenWeatherLocationResponse[] locations)
    {
        openWeatherClient.GetLocationsAsync(query, Arg.Any<CancellationToken>()).Returns(locations);
    }
}
