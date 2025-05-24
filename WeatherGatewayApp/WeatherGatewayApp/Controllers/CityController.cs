using Microsoft.AspNetCore.Mvc;
using WeatherGatewayApp.Application.Cities;
using WeatherGatewayApp.Application.Contracts;

namespace WeatherProviderApp.Controllers
{
    [ApiController]
    [Route("cities")]
    public class CityController : ControllerBase
    {
        private readonly ICityAppService appService;

        public CityController(ICityAppService appService)
        {
            this.appService = appService;
        }

        /// <summary>Retorna todas as cidades cadastradas no sistema.</summary>
        [HttpGet]
        public async Task<ActionResult<List<CityDto>>> GetAll(Guid userId)
        {
            var result = await appService.GetAllAsync(userId);
            return Ok(result);
        }

        /// <summary>Retorna todas as cidades favoritas no sistema.</summary>
        [HttpGet("favorites")]
        public async Task<ActionResult<List<CityDto>>> GetAllFavorites(Guid userId)
        {
            var result = await appService.GetAllFavoritesAsync(userId);
            return Ok(result);
        }

        /// <summary>Favorita/Desfavorita a cidade informada.</summary>
        /// <param name="id">Id da cidade</param>
        [HttpPatch("{id}/favorite")]
        public async Task<IActionResult> ToggleFavorite(Guid id)
        {
            var city = await appService.ToggleFavoriteAsync(id);

            if (city == null)
            {
                return NotFound();
            }

            return Ok(city);
        }

        /// <summary>Remover cidade informada.</summary>
        /// <param name="id">Id da cidade</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            appService.DeleteAsync(id);
            return Ok();
        }
    }
}
