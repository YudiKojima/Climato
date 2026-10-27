using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Exceptions;

namespace Climato.Gateway.UnitTests.Domain;

public class CityTests
{
    [Fact]
    public void Constructor_NovaCidade_NaoFavoritaComIdEDataDeCriacao()
    {
        var userId = Guid.NewGuid();

        var city = new City("Lima", "PE", userId);

        city.Id.ShouldNotBe(Guid.Empty);
        city.UserId.ShouldBe(userId);
        city.IsFavorite.ShouldBeFalse();
        city.CreatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ToggleFavorite_DuasVezes_VoltaAoEstadoInicial()
    {
        var city = new City("Lima", "PE", Guid.NewGuid());

        city.ToggleFavorite();
        city.IsFavorite.ShouldBeTrue();

        city.ToggleFavorite();
        city.IsFavorite.ShouldBeFalse();
    }

    [Fact]
    public void EnsureCanBeDeleted_CidadeFavorita_LancaDomainException()
    {
        var city = new City("Lima", "PE", Guid.NewGuid());
        city.ToggleFavorite();

        var exception = Should.Throw<DomainException>(() => city.EnsureCanBeDeleted());

        exception.Message.ShouldBe("Não é possível remover uma cidade favorita.");
    }

    [Fact]
    public void EnsureCanBeDeleted_CidadeNaoFavorita_NaoLanca()
    {
        var city = new City("Lima", "PE", Guid.NewGuid());

        Should.NotThrow(() => city.EnsureCanBeDeleted());
    }
}
