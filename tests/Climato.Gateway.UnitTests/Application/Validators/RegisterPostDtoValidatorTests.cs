using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class RegisterPostDtoValidatorTests
{
    private readonly RegisterPostDtoValidator validator = new();

    [Fact]
    public void Validate_DadosValidos_SemErros()
    {
        var result = validator.Validate(new RegisterPostDto { Email = "ana@teste.dev", Name = "Ana", Password = "senha-segura" });

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Ana", "senha-segura", "Informe o e-mail.")]
    [InlineData("nao-e-email", "Ana", "senha-segura", "E-mail inválido.")]
    [InlineData("ana@teste.dev", "", "senha-segura", "Informe o nome.")]
    [InlineData("ana@teste.dev", "Ana", "", "Informe a senha.")]
    [InlineData("ana@teste.dev", "Ana", "curta", "A senha deve ter entre 8 e 128 caracteres.")]
    public void Validate_DadoInvalido_DevolveMensagemEmPortugues(string email, string name, string password, string message)
    {
        var result = validator.Validate(new RegisterPostDto { Email = email, Name = name, Password = password });

        result.Errors.Select(error => error.ErrorMessage).ShouldContain(message);
    }
}
