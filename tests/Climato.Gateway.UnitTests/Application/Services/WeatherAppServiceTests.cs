using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.Application.Services;

namespace Climato.Gateway.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private readonly IWeatherProviderClient providerClient = Substitute.For<IWeatherProviderClient>();
    private readonly WeatherAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(providerClient);
    }

    [Fact]
    public async Task GetByCityAsync_ProviderNaoEncontra_DevolveNull()
    {
        providerClient.GetWeatherAsync("Atlantida", true, Arg.Any<CancellationToken>()).Returns((WeatherDto?)null);

        var weather = await appService.GetByCityAsync("Atlantida", includeImage: true, cancellationToken);

        weather.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCityAsync_CidadeEncontrada_DevolveOClimaSemGravarNoHistorico()
    {
        var expected = TestData.Weather("Lima");
        providerClient.GetWeatherAsync("lima", true, Arg.Any<CancellationToken>()).Returns(expected);

        var weather = await appService.GetByCityAsync("lima", includeImage: true, cancellationToken);

        weather.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetByCityAsync_SemImagem_RepassaAoProvider()
    {
        var expected = TestData.Weather("Lima");
        providerClient.GetWeatherAsync("Lima", false, Arg.Any<CancellationToken>()).Returns(expected);

        var weather = await appService.GetByCityAsync("Lima", includeImage: false, cancellationToken);

        weather.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetForecastAsync_RepassaARespostaDoProvider()
    {
        var forecast = new ForecastDto { City = "Lima", Country = "PE", Days = [] };
        providerClient.GetForecastAsync("Lima", Arg.Any<CancellationToken>()).Returns(forecast);

        var result = await appService.GetForecastAsync("Lima", cancellationToken);

        result.ShouldBeSameAs(forecast);
    }
}
