using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class CityPostDtoValidatorTests
{
    private readonly CityPostDtoValidator validator = new();

    [Fact]
    public void Validate_DadosValidos_NaoTemErros()
    {
        validator.Validate(new CityPostDto { Name = "Lima" }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NomeVazio_DevolveMensagem()
    {
        var result = validator.Validate(new CityPostDto { Name = "" });

        result.Errors.ShouldContain(error => error.PropertyName == "Name" && error.ErrorMessage == "Informe o nome da cidade.");
    }

    [Fact]
    public void Validate_NomeMaiorQue100_DevolveMensagem()
    {
        var result = validator.Validate(new CityPostDto { Name = new string('a', 101) });

        result.Errors.ShouldContain(error => error.ErrorMessage == "O nome da cidade deve ter no máximo 100 caracteres.");
    }
}
