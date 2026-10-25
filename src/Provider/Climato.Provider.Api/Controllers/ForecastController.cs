using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Weathers;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Controllers;

[ApiController]
[Route("forecasts")]
public class ForecastController : ControllerBase
{
    private readonly IForecastAppService appService;

    public ForecastController(IForecastAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna a previsão de até 5 dias da cidade informada, agregada por dia no fuso da cidade.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Previsão diária: mínima, máxima, condição do meio-dia, chance de chuva (%) e vento máximo em km/h.</response>
    /// <response code="400">Parâmetro 'cityName' ausente.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">OpenWeather indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Forecast>> Get([FromQuery] string cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Parâmetro 'cityName' é obrigatório.");
        }

        var forecast = await appService.GetByCityAsync(cityName, cancellationToken);

        if (forecast == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(forecast);
    }
}
