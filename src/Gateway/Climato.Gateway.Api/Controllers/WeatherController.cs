using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("weathers")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna os dados climáticos da cidade informada e a registra no histórico do usuário autenticado.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    [HttpGet("{cityName}")]
    public async Task<ActionResult<WeatherDto>> GetWeather(string cityName, CancellationToken cancellationToken)
    {
        var weather = await appService.GetByCityAsync(cityName, User.GetUserId(), cancellationToken);

        if (weather == null)
        {
            return NotFound();
        }

        return Ok(weather);
    }
}
