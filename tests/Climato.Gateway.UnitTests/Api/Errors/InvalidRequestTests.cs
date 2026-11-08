using Climato.Gateway.Api.Errors;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Climato.Gateway.UnitTests.Api.Errors;

public class InvalidRequestTests
{
    [Fact]
    public void Sanitize_ErroDeJson_ViraJsonInvalidoNoCorpo()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("$.email", "The JSON value could not be converted to System.String. Path: $.email");
        modelState.AddModelError("dto", "The dto field is required.");

        var result = InvalidRequest.Sanitize(modelState, ["dto"]);

        result.Keys.ShouldBe(["body"]);
        result["body"]!.Errors.Select(e => e.ErrorMessage).ShouldBe(["JSON inválido."]);
    }

    [Fact]
    public void Sanitize_CorpoVazio_ViraSoJsonInvalido()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("", "A non-empty request body is required.");
        modelState.AddModelError("dto", "The dto field is required.");

        var result = InvalidRequest.Sanitize(modelState, ["dto"]);

        result.Keys.ShouldBe(["body"]);
    }

    [Fact]
    public void Sanitize_ValorDeRotaOuQuery_ViraValorInvalidoNaMesmaChave()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("id", "The value 'abc' is not valid.");

        var result = InvalidRequest.Sanitize(modelState, []);

        result["id"]!.Errors.Select(e => e.ErrorMessage).ShouldBe(["Valor inválido."]);
    }
}
