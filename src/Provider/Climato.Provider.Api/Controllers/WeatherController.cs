using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Weathers;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Controllers;

[ApiController]
[Route("weathers")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna o clima atual e a imagem da cidade informada.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Clima atual, com vento em km/h e horários no fuso da cidade.</response>
    /// <response code="400">Parâmetro 'cityName' ausente.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">OpenWeather indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Weather>> Get([FromQuery] string cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Parâmetro 'cityName' é obrigatório.");
        }

        var weather = await appService.GetByCityAsync(cityName, cancellationToken);

        if (weather == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(weather);
    }
}
