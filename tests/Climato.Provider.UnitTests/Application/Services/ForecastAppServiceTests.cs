using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Weathers;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class ForecastAppServiceTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly ForecastAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public ForecastAppServiceTests()
    {
        appService = new ForecastAppService(openWeatherClient, cache);
    }

    [Fact]
    public async Task GetByCityAsync_FusoNegativo_AgrupaPelaDataLocal()
    {
        ReturnsForecast(Forecast(TimeSpan.FromHours(-3),
            Item(Utc(2026, 10, 26, 2)),
            Item(Utc(2026, 10, 26, 15)),
            Item(Utc(2026, 10, 27, 2))));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.Select(day => day.Date).ShouldBe(new[] { new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 26) });
    }

    [Fact]
    public async Task GetByCityAsync_FusoPositivo_AgrupaPelaDataLocalEmOrdem()
    {
        ReturnsForecast(Forecast(TimeSpan.FromHours(9),
            Item(Utc(2026, 10, 26, 16)),
            Item(Utc(2026, 10, 26, 3)),
            Item(Utc(2026, 10, 25, 14))));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.Select(day => day.Date).ShouldBe(new[] { new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 27) });
    }

    [Fact]
    public async Task GetByCityAsync_PontosDoMesmoDia_AgregaMinMaxChuvaVentoECondicaoDoMeioDia()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero,
            Item(Utc(2026, 10, 26, 9), tempMin: 18, tempMax: 22, pop: 0.1, windSpeed: 2.0),
            Item(Utc(2026, 10, 26, 12), tempMin: 17, tempMax: 26, pop: 0.456, windSpeed: 5.5, description: "chuva leve", icon: "10d"),
            Item(Utc(2026, 10, 26, 15), tempMin: 19, tempMax: 24, pop: 0.3, windSpeed: 3.0)));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        var day = forecast.Days.ShouldHaveSingleItem();
        day.TempMin.ShouldBe(17);
        day.TempMax.ShouldBe(26);
        day.PrecipitationProbability.ShouldBe(46);
        day.WindSpeedKmh.ShouldBe(19.8);
        day.Description.ShouldBe("chuva leve");
        day.Icon.ShouldBe("10d");
    }

    [Fact]
    public async Task GetByCityAsync_SemPontoAoMeioDia_UsaACondicaoDoPontoMaisProximo()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero,
            Item(Utc(2026, 10, 26, 6), description: "madrugada"),
            Item(Utc(2026, 10, 26, 13), description: "tarde"),
            Item(Utc(2026, 10, 26, 21), description: "noite")));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.ShouldHaveSingleItem().Description.ShouldBe("tarde");
    }

    [Fact]
    public async Task GetByCityAsync_SeteDias_DevolveOsCincoPrimeiros()
    {
        var items = Enumerable.Range(0, 7)
            .Select(offset => Item(Utc(2026, 10, 26, 12).AddDays(offset)))
            .ToArray();
        ReturnsForecast(Forecast(TimeSpan.Zero, items));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.Count.ShouldBe(5);
        forecast.Days[0].Date.ShouldBe(new DateOnly(2026, 10, 26));
        forecast.Days[4].Date.ShouldBe(new DateOnly(2026, 10, 30));
    }

    [Fact]
    public async Task GetByCityAsync_PrevisaoNoCache_NaoChamaAOpenWeather()
    {
        var cached = new Forecast { City = "São Paulo", Country = "BR", Days = [] };
        cache.GetAsync<Forecast>("previsao", "São Paulo", Arg.Any<CancellationToken>()).Returns(cached);

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldBeSameAs(cached);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetForecastAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeInexistente_DevolveNullSemGravarNoCache()
    {
        openWeatherClient.GetForecastAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((OpenWeatherForecastResponse?)null);

        var forecast = await appService.GetByCityAsync("Atlântida", cancellationToken);

        forecast.ShouldBeNull();
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Forecast>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_Sucesso_GravaNoCacheDePrevisaoPorTrintaMinutos()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero, Item(Utc(2026, 10, 26, 12))));

        await appService.GetByCityAsync("São Paulo", cancellationToken);

        await cache.Received(1).SetAsync("previsao", "São Paulo", Arg.Any<Forecast>(), TimeSpan.FromMinutes(30), Arg.Any<CancellationToken>());
    }

    private void ReturnsForecast(OpenWeatherForecastResponse response)
    {
        openWeatherClient.GetForecastAsync("São Paulo", Arg.Any<CancellationToken>()).Returns(response);
    }
}
