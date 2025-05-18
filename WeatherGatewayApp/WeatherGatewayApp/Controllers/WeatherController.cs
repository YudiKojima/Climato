using Microsoft.AspNetCore.Mvc;
using WeatherGatewayApp.Application.Contracts;

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

        /// <summary>Retorna os dados climáticos da cidade informada.</summary>
        /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
        [HttpGet("{cityName}")]
        public async Task<IActionResult> GetWeather(string cityName)
        {
            var weather = await appService.GetByCityAsync(cityName);

            if (weather == null)
            {
                return NotFound();
            }

            return Ok(weather);
        }
    }
}
