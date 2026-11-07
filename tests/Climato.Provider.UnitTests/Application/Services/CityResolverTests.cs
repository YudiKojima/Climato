using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class CityResolverTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly CityResolver resolver;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public CityResolverTests()
    {
        resolver = new CityResolver(openWeatherClient, cache);
    }

    [Fact]
    public async Task ResolveAsync_ComNomeEmPortugues_DevolveNomePtNomeOriginalECoordenadas()
    {
        ReturnsLocations("Tóquio", Location("Tokyo", "Tóquio", "JP", lat: 35.68, lon: 139.76));

        var city = await resolver.ResolveAsync("Tóquio", cancellationToken);

        city.ShouldBe(new ResolvedCity("Tóquio", "Tokyo", "JP", 35.68, 139.76));
    }

    [Fact]
    public async Task ResolveAsync_SemNomeEmPortugues_UsaONomeOriginal()
    {
        ReturnsLocations("Lima", Location("Lima", country: "PE"));

        var city = await resolver.ResolveAsync("Lima", cancellationToken);

        city.ShouldNotBeNull();
        city.Name.ShouldBe("Lima");
        city.OriginalName.ShouldBe("Lima");
    }

    [Fact]
    public async Task ResolveAsync_VariosResultados_UsaOPrimeiro()
    {
        ReturnsLocations("Springfield",
            Location("Springfield", country: "US", state: "Illinois", lat: 39.8),
            Location("Springfield", country: "US", state: "Missouri", lat: 37.2));

        var city = await resolver.ResolveAsync("Springfield", cancellationToken);

        city.ShouldNotBeNull();
        city.Lat.ShouldBe(39.8);
    }

    [Fact]
    public async Task ResolveAsync_SemResultado_DevolveNullSemGravarNoCache()
    {
        ReturnsLocations("Atlântida");

        var city = await resolver.ResolveAsync("Atlântida", cancellationToken);

        city.ShouldBeNull();
        await cache.DidNotReceiveWithAnyArgs().SetAsync<ResolvedCity>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_CidadeNoCache_NaoChamaAOpenWeather()
    {
        var cached = new ResolvedCity("Lima", "Lima", "PE", -12.04, -77.03);
        cache.GetAsync<ResolvedCity>("cidades", "Lima", Arg.Any<CancellationToken>()).Returns(cached);

        var city = await resolver.ResolveAsync("Lima", cancellationToken);

        city.ShouldBeSameAs(cached);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetLocationsAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_Sucesso_GravaNoCacheDeCidadesPorVinteEQuatroHoras()
    {
        ReturnsLocations("Lima", Location("Lima", country: "PE"));

        await resolver.ResolveAsync("Lima", cancellationToken);

        await cache.Received(1).SetAsync("cidades", "Lima", Arg.Any<ResolvedCity>(), TimeSpan.FromHours(24), Arg.Any<CancellationToken>());
    }

    private void ReturnsLocations(string query, params OpenWeatherLocationResponse[] locations)
    {
        openWeatherClient.GetLocationsAsync(query, Arg.Any<CancellationToken>()).Returns(locations);
    }
}
