using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class LocationSearchQueryValidatorTests
{
    private const string Message = "Parâmetro 'query' deve ter entre 2 e 100 caracteres.";

    private readonly LocationSearchQueryValidator validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  a  ")]
    public void Validate_QueryAusenteOuCurta_DevolveMensagem(string? query)
    {
        var result = validator.Validate(new LocationSearchQuery { Query = query });

        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(Message);
    }

    [Fact]
    public void Validate_QueryLonga_DevolveMensagem()
    {
        var result = validator.Validate(new LocationSearchQuery { Query = new string('a', 101) });

        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(Message);
    }

    [Theory]
    [InlineData("Li")]
    [InlineData("  Lima  ")]
    public void Validate_QueryValida_SemErros(string query)
    {
        validator.Validate(new LocationSearchQuery { Query = query }).IsValid.ShouldBeTrue();
    }
}
