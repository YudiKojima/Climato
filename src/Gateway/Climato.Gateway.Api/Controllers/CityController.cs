using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

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
    public async Task<ActionResult<List<CityDto>>> GetAll(Guid userId, CancellationToken cancellationToken)
    {
        var result = await appService.GetAllAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna todas as cidades favoritas no sistema.</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<List<CityDto>>> GetAllFavorites(Guid userId, CancellationToken cancellationToken)
    {
        var result = await appService.GetAllFavoritesAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Favorita/Desfavorita a cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    [HttpPatch("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id, CancellationToken cancellationToken)
    {
        var city = await appService.ToggleFavoriteAsync(id, cancellationToken);

        if (city == null)
        {
            return NotFound();
        }

        return Ok(city);
    }

    /// <summary>Remover cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        appService.DeleteAsync(id);
        return Ok();
    }
}
