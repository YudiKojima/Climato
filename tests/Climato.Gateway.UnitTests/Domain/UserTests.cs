using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.UnitTests.Domain;

public class UserTests
{
    [Theory]
    [InlineData("  Ana@Teste.DEV ", "ana@teste.dev")]
    [InlineData("ana@teste.dev", "ana@teste.dev")]
    public void NormalizeEmail_EmailComEspacosEMaiusculas_DevolveMinusculoSemEspacos(string email, string expected)
    {
        User.NormalizeEmail(email).ShouldBe(expected);
    }

    [Fact]
    public void Constructor_NovoUsuario_NormalizaEmailEAparaONome()
    {
        var user = new User(" Ana@Teste.dev ", "  Ana Souza ");

        user.Id.ShouldNotBe(Guid.Empty);
        user.Email.ShouldBe("ana@teste.dev");
        user.Name.ShouldBe("Ana Souza");
    }
}
