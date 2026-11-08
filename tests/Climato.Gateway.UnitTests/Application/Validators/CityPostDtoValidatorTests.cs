using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class CityPostDtoValidatorTests
{
    private readonly CityPostDtoValidator validator = new();

    [Fact]
    public void Validate_DadosValidos_NaoTemErros()
    {
        validator.Validate(new CityPostDto { Name = "Lima", Country = "PE" }).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "PE", "Name", "Informe o nome da cidade.")]
    [InlineData("Lima", "", "Country", "Informe o país.")]
    public void Validate_CampoVazio_DevolveMensagem(string name, string country, string property, string message)
    {
        var result = validator.Validate(new CityPostDto { Name = name, Country = country });

        result.Errors.ShouldContain(error => error.PropertyName == property && error.ErrorMessage == message);
    }

    [Fact]
    public void Validate_NomeMaiorQue255_DevolveMensagem()
    {
        var result = validator.Validate(new CityPostDto { Name = new string('a', 256), Country = "PE" });

        result.Errors.ShouldContain(error => error.ErrorMessage == "O nome da cidade deve ter no máximo 255 caracteres.");
    }
}
