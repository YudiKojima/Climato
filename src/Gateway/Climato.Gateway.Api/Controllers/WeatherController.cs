using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("weathers")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna os dados climáticos da cidade informada.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="userId">Id do usuário que pesquisou</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    [HttpGet("{cityName}")]
    public async Task<ActionResult<WeatherDto>> GetWeather(string cityName, [FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        var weather = await appService.GetByCityAsync(cityName, userId, cancellationToken);

        if (weather == null)
        {
            return NotFound();
        }

        return Ok(weather);
    }
}
