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
    [HttpGet]
    public async Task<ActionResult<Weather>> Get([FromQuery] string cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return BadRequest("Parâmetro 'cityName' é obrigatório.");
        }

        try
        {
            var result = await appService.GetByCityAsync(cityName, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException)
        {
            return NotFound($"Não foi possível obter dados para a cidade '{cityName}'.");
        }
    }
}
