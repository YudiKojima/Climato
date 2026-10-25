using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("forecasts")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public ForecastController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna a previsão de até 5 dias da cidade informada, agregada por dia no fuso da cidade. Não registra a cidade no histórico.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Previsão diária: mínima, máxima, condição do meio-dia, chance de chuva (%) e vento máximo em km/h.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet("{cityName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ForecastDto>> GetForecast(string cityName, CancellationToken cancellationToken)
    {
        var forecast = await appService.GetForecastAsync(cityName, cancellationToken);

        if (forecast == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(forecast);
    }
}
