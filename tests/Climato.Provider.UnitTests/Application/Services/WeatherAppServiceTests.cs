using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Weathers;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private static readonly ResolvedCity saoPaulo = new("São Paulo", "Sao Paulo", "BR", -23.55, -46.63);
    private static readonly ResolvedCity lima = new("Lima", "Lima", "PE", -12.04, -77.03);

    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly ICityResolver cityResolver = Substitute.For<ICityResolver>();
    private readonly ICityImageAppService cityImageAppService = Substitute.For<ICityImageAppService>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly WeatherAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(openWeatherClient, cityResolver, cityImageAppService, cache);
    }

    [Fact]
    public async Task GetByCityAsync_RespostaDaOpenWeather_ConverteParaOContrato()
    {
        Resolves("sao paulo", saoPaulo);
        openWeatherClient.GetCurrentAsync(-23.55, -46.63, Arg.Any<CancellationToken>())
            .Returns(Current("Sé", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30), windSpeed: 5.0));
        cityImageAppService.GetImageUrlAsync("Sao Paulo", Arg.Any<CancellationToken>()).Returns("https://images.pexels.com/sp.jpg");

        var weather = await appService.GetByCityAsync("sao paulo", cancellationToken);

        weather.ShouldNotBeNull();
        weather.City.ShouldBe("São Paulo");
        weather.Country.ShouldBe("BR");
        weather.Description.ShouldBe("céu limpo");
        weather.WindSpeedKmh.ShouldBe(18.0);
        weather.Sunrise.Offset.ShouldBe(TimeSpan.FromHours(-3));
        weather.Sunrise.DateTime.ShouldBe(new DateTime(2026, 10, 26, 5, 45, 0));
        weather.Sunset.DateTime.ShouldBe(new DateTime(2026, 10, 26, 18, 30, 0));
        weather.ImageUrl.ShouldBe("https://images.pexels.com/sp.jpg");
    }

    [Fact]
    public async Task GetByCityAsync_CidadeResolvida_UsaNomeEPaisDoGeocoding()
    {
        Resolves("Lima", lima);
        openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>())
            .Returns(Current("Miraflores", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldNotBeNull();
        weather.City.ShouldBe("Lima");
        weather.Country.ShouldBe("PE");
    }

    [Fact]
    public async Task GetByCityAsync_PexelsSemImagem_DevolveClimaSemImagem()
    {
        Resolves("Lima", lima);
        openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>())
            .Returns(Current("Lima", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));
        cityImageAppService.GetImageUrlAsync("Lima", Arg.Any<CancellationToken>()).Returns((string?)null);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldNotBeNull();
        weather.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCityAsync_ClimaNoCache_NaoResolveNemChamaAOpenWeather()
    {
        var cached = new Weather
        {
            City = "Lima", Country = "PE", Description = "nublado", Icon = "04d",
            Temperature = 18, FeelsLike = 18, TempMin = 16, TempMax = 20, Humidity = 80, Pressure = 1012,
            WindSpeedKmh = 10, WindDirection = 180,
            Sunrise = Utc(2026, 10, 26, 10), Sunset = Utc(2026, 10, 26, 23)
        };
        cache.GetAsync<Weather>("clima", "Lima", Arg.Any<CancellationToken>()).Returns(cached);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldBeSameAs(cached);
        await cityResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, cancellationToken);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetCurrentAsync(default, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeNaoResolvida_DevolveNullSemConsultarClimaNemGravarCache()
    {
        cityResolver.ResolveAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((ResolvedCity?)null);

        var weather = await appService.GetByCityAsync("Atlântida", cancellationToken);

        weather.ShouldBeNull();
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetCurrentAsync(default, default, cancellationToken);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Weather>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_ClimaNaoEncontrado_DevolveNullSemImagemESemCache()
    {
        Resolves("Lima", lima);
        openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>()).Returns((OpenWeatherCurrentResponse?)null);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldBeNull();
        await cityImageAppService.DidNotReceiveWithAnyArgs().GetImageUrlAsync(default!, cancellationToken);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Weather>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_Sucesso_GravaNoCacheDeClimaPorDezMinutosComOTextoPesquisado()
    {
        Resolves("sao paulo", saoPaulo);
        openWeatherClient.GetCurrentAsync(-23.55, -46.63, Arg.Any<CancellationToken>())
            .Returns(Current("Sé", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30)));

        await appService.GetByCityAsync("sao paulo", cancellationToken);

        await cache.Received(1).SetAsync("clima", "sao paulo", Arg.Any<Weather>(), TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    private void Resolves(string cityName, ResolvedCity city)
    {
        cityResolver.ResolveAsync(cityName, Arg.Any<CancellationToken>()).Returns(city);
    }
}
