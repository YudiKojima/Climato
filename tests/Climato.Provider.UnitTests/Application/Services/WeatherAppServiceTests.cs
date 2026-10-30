using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Weathers;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly ICityImageAppService cityImageAppService = Substitute.For<ICityImageAppService>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly WeatherAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(openWeatherClient, cityImageAppService, cache);
    }

    [Fact]
    public async Task GetByCityAsync_RespostaDaOpenWeather_ConverteParaOContrato()
    {
        openWeatherClient.GetCurrentAsync("sao paulo", Arg.Any<CancellationToken>())
            .Returns(Current("São Paulo", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30), windSpeed: 5.0));
        cityImageAppService.GetImageUrlAsync("São Paulo", Arg.Any<CancellationToken>()).Returns("https://images.pexels.com/sp.jpg");

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
    public async Task GetByCityAsync_PexelsSemImagem_DevolveClimaSemImagem()
    {
        openWeatherClient.GetCurrentAsync("Lima", Arg.Any<CancellationToken>())
            .Returns(Current("Lima", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));
        cityImageAppService.GetImageUrlAsync("Lima", Arg.Any<CancellationToken>()).Returns((string?)null);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldNotBeNull();
        weather.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCityAsync_ClimaNoCache_NaoChamaAOpenWeather()
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
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetCurrentAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeInexistente_DevolveNullSemImagemESemCache()
    {
        openWeatherClient.GetCurrentAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((OpenWeatherCurrentResponse?)null);

        var weather = await appService.GetByCityAsync("Atlântida", cancellationToken);

        weather.ShouldBeNull();
        await cityImageAppService.DidNotReceiveWithAnyArgs().GetImageUrlAsync(default!, cancellationToken);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Weather>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_Sucesso_GravaNoCacheDeClimaPorDezMinutosComOTextoPesquisado()
    {
        openWeatherClient.GetCurrentAsync("sao paulo", Arg.Any<CancellationToken>())
            .Returns(Current("São Paulo", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30)));

        await appService.GetByCityAsync("sao paulo", cancellationToken);

        await cache.Received(1).SetAsync("clima", "sao paulo", Arg.Any<Weather>(), TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }
}
