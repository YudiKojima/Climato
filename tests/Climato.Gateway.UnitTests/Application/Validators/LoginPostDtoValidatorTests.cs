using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class LoginPostDtoValidatorTests
{
    private readonly LoginPostDtoValidator validator = new();

    [Theory]
    [InlineData("", "senha", "Informe o e-mail.")]
    [InlineData("ana@teste.dev", "", "Informe a senha.")]
    public void Validate_CampoVazio_DevolveMensagemEmPortugues(string email, string password, string message)
    {
        var result = validator.Validate(new LoginPostDto { Email = email, Password = password });

        result.Errors.Select(error => error.ErrorMessage).ShouldContain(message);
    }

    [Fact]
    public void Validate_DadosPreenchidos_SemErros()
    {
        validator.Validate(new LoginPostDto { Email = "ana@teste.dev", Password = "qualquer" }).IsValid.ShouldBeTrue();
    }
}
