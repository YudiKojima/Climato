using Microsoft.AspNetCore.Mvc;
using WeatherProviderApp.Application.Contracts;
using WeatherProviderApp.Domain.Weathers;

namespace WeatherProviderApp.Controllers
{
    [ApiController]
    [Route("weathers")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherAppService appService;

        public WeatherController(IWeatherAppService appService)
        {
            this.appService = appService;
        }

        [HttpGet]
        public async Task<ActionResult<Weather>> Get([FromQuery] string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                return BadRequest("Parâmetro 'city' é obrigatório.");
            }

            try
            {
                var result = await appService.GetByCityAsync(cityName);
                return Ok(result);
            }
            catch (HttpRequestException)
            {
                return NotFound($"Não foi possível obter dados para a cidade '{cityName}'.");
            }
        }
    }
}
