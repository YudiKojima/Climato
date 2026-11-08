using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Exceptions;

namespace Climato.Gateway.UnitTests.Application.Services;

public class CityAppServiceTests
{
    private readonly ICityRepository repository = Substitute.For<ICityRepository>();
    private readonly IWeatherProviderClient providerClient = Substitute.For<IWeatherProviderClient>();
    private readonly CityAppService appService;
    private readonly Guid userId = Guid.NewGuid();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public CityAppServiceTests()
    {
        appService = new CityAppService(repository, providerClient, TestMapper.Create());
    }

    [Fact]
    public async Task DeleteAsync_CidadeNaoEncontradaParaOUsuario_DevolveFalseSemRemover()
    {
        var cityId = Guid.NewGuid();
        repository.GetAsync(cityId, userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var deleted = await appService.DeleteAsync(cityId, userId, cancellationToken);

        deleted.ShouldBeFalse();
        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_CidadeFavorita_LancaDomainExceptionSemRemover()
    {
        var city = new City("Lima", "PE", userId);
        city.ToggleFavorite();
        repository.GetAsync(city.Id, userId, Arg.Any<CancellationToken>()).Returns(city);

        await Should.ThrowAsync<DomainException>(() => appService.DeleteAsync(city.Id, userId, cancellationToken));

        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_CidadeDoUsuario_RemoveEDevolveTrue()
    {
        var city = new City("Lima", "PE", userId);
        repository.GetAsync(city.Id, userId, Arg.Any<CancellationToken>()).Returns(city);

        var deleted = await appService.DeleteAsync(city.Id, userId, cancellationToken);

        deleted.ShouldBeTrue();
        await repository.Received(1).DeleteAsync(city, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleFavoriteAsync_CidadeNaoEncontradaParaOUsuario_DevolveNullSemAtualizar()
    {
        var cityId = Guid.NewGuid();
        repository.GetAsync(cityId, userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var result = await appService.ToggleFavoriteAsync(cityId, userId, cancellationToken);

        result.ShouldBeNull();
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_CidadeDoUsuario_AlternaSalvaEDevolveODto()
    {
        var city = new City("Lima", "PE", userId);
        repository.GetAsync(city.Id, userId, Arg.Any<CancellationToken>()).Returns(city);

        var result = await appService.ToggleFavoriteAsync(city.Id, userId, cancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(city.Id);
        result.Name.ShouldBe("Lima");
        result.IsFavorite.ShouldBeTrue();
        await repository.Received(1).UpdateAsync(city, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_CidadesDoUsuario_DevolveOsDtos()
    {
        repository.GetAllAsync(userId, Arg.Any<CancellationToken>()).Returns([new City("Lima", "PE", userId), new City("Quito", "EC", userId)]);

        var cities = await appService.GetAllAsync(userId, cancellationToken);

        cities.Select(city => city.Name).ShouldBe(new[] { "Lima", "Quito" });
    }
    [Fact]
    public async Task AddAsync_CidadeNova_GravaComONomeEOPaisResolvidosPeloProvider()
    {
        providerClient.GetWeatherAsync("lima", false, Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima", "PE"));
        repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var result = await appService.AddAsync(new CityPostDto { Name = " lima " }, userId, cancellationToken);

        result.HasValue.ShouldBeTrue();
        result.Value.Created.ShouldBeTrue();
        result.Value.City.Name.ShouldBe("Lima");
        result.Value.City.Country.ShouldBe("PE");
        await repository.Received(1).CreateAsync(
            Arg.Is<City>(c => c.Name == "Lima" && c.Country == "PE" && c.UserId == userId && !c.IsFavorite),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_CidadeJaNoHistorico_DevolveAExistenteSemCriar()
    {
        var existing = new City("Lima", "PE", userId);
        existing.ToggleFavorite();
        providerClient.GetWeatherAsync("Lima", false, Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima", "PE"));
        repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await appService.AddAsync(new CityPostDto { Name = "Lima" }, userId, cancellationToken);

        result.HasValue.ShouldBeTrue();
        result.Value.Created.ShouldBeFalse();
        result.Value.City.Id.ShouldBe(existing.Id);
        result.Value.City.IsFavorite.ShouldBeTrue();
        await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task AddAsync_CidadeInexistenteNoProvider_DevolveNullSemGravar()
    {
        providerClient.GetWeatherAsync("Cidade Inventada", false, Arg.Any<CancellationToken>()).Returns((WeatherDto?)null);

        var result = await appService.AddAsync(new CityPostDto { Name = "Cidade Inventada" }, userId, cancellationToken);

        result.HasValue.ShouldBeFalse();
        repository.ReceivedCalls().ShouldBeEmpty();
    }
}
