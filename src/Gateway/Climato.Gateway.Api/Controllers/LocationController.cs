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

    /// <summary>Busca cidades pelo nome (até 5 resultados). A OpenWeather casa o nome inteiro, não prefixo.</summary>
    /// <param name="request">Nome da cidade em `query` (2 a 100 caracteres), exemplo: São Paulo</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidades encontradas, com nome em português quando houver; lista vazia sem resultado.</response>
    /// <response code="400">Parâmetro 'query' com menos de 2 ou mais de 100 caracteres.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> Search([FromQuery] LocationSearchQuery request, CancellationToken cancellationToken)
    {
        var locations = await appService.SearchAsync(request.Query!, cancellationToken);
        return Ok(locations);
    }
}
