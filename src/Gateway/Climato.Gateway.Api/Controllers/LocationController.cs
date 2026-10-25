using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("locations")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationAppService appService;

    public LocationController(ILocationAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Busca cidades pelo nome para o autocomplete (até 5 resultados).</summary>
    /// <param name="query">Parte do nome da cidade (2 a 100 caracteres), exemplo: São Pa</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidades encontradas, com nome em português quando houver; lista vazia sem resultado.</response>
    /// <response code="400">Parâmetro 'query' com menos de 2 ou mais de 100 caracteres.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> Search([FromQuery] LocationSearchQuery query, CancellationToken cancellationToken)
    {
        var locations = await appService.SearchAsync(query.Query!, cancellationToken);
        return Ok(locations);
    }
}
