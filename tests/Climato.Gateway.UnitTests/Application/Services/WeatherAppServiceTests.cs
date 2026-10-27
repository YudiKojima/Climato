using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Cities;

namespace Climato.Gateway.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private readonly ICityRepository repository = Substitute.For<ICityRepository>();
    private readonly IWeatherProviderClient providerClient = Substitute.For<IWeatherProviderClient>();
    private readonly WeatherAppService appService;
    private readonly Guid userId = Guid.NewGuid();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(repository, providerClient);
    }

    [Fact]
    public async Task GetByCityAsync_ProviderNaoEncontra_DevolveNullSemTocarNoRepositorio()
    {
        providerClient.GetWeatherAsync("Atlantida", Arg.Any<CancellationToken>()).Returns((WeatherDto?)null);

        var weather = await appService.GetByCityAsync("Atlantida", userId, cancellationToken);

        weather.ShouldBeNull();
        repository.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByCityAsync_CidadeNova_CriaNoHistoricoComONomeDevolvidoPeloProvider()
    {
        providerClient.GetWeatherAsync("lima", Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima"));
        repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var weather = await appService.GetByCityAsync("lima", userId, cancellationToken);

        weather.ShouldNotBeNull();
        await repository.Received(1).CreateAsync(
            Arg.Is<City>(city => city.Name == "Lima" && city.Country == "PE" && city.UserId == userId && !city.IsFavorite),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByCityAsync_CidadeJaNoHistorico_NaoCriaDeNovo()
    {
        providerClient.GetWeatherAsync("Lima", Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima"));
        repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns(new City("Lima", "PE", userId));

        await appService.GetByCityAsync("Lima", userId, cancellationToken);

        await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken);
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
