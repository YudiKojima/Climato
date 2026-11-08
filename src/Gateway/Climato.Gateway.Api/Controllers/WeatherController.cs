using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Api.RateLimiting;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[EnableRateLimiting(RateLimitPolicies.Api)]
[Route("weathers")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna os dados climáticos da cidade informada.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="image">false omite a foto da cidade (usado pelos cards)</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Clima atual, com vento em km/h e horários no fuso da cidade.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet("{cityName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WeatherDto>> GetWeather(string cityName, [FromQuery] bool image = true, CancellationToken cancellationToken = default)
    {
        var weather = await appService.GetByCityAsync(cityName, image, cancellationToken);

        if (weather == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(weather);
    }
}
