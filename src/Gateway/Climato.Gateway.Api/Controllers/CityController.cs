using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("cities")]
[Authorize]
public class CityController : ControllerBase
{
    private readonly ICityAppService appService;

    public CityController(ICityAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna as cidades pesquisadas pelo usuário autenticado.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CityDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await appService.GetAllAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna as cidades favoritas do usuário autenticado.</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<List<CityDto>>> GetAllFavorites(CancellationToken cancellationToken)
    {
        var result = await appService.GetAllFavoritesAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Favorita/Desfavorita a cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidade atualizada.</response>
    /// <response code="404">Cidade não encontrada para o usuário.</response>
    [HttpPatch("{id}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityDto>> ToggleFavorite(Guid id, CancellationToken cancellationToken)
    {
        var city = await appService.ToggleFavoriteAsync(id, User.GetUserId(), cancellationToken);

        if (city == null)
        {
            return NotFound();
        }

        return Ok(city);
    }

    /// <summary>Remove a cidade informada do histórico.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Cidade removida.</response>
    /// <response code="404">Cidade não encontrada para o usuário.</response>
    /// <response code="409">Cidade favorita não pode ser removida.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(id, User.GetUserId(), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
