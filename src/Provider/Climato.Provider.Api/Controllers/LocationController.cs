using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Locations;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Controllers;

[ApiController]
[Route("locations")]
public class LocationController : ControllerBase
{
    private const int MinQueryLength = 2;
    private const int MaxQueryLength = 100;

    private readonly ILocationAppService appService;

    public LocationController(ILocationAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Busca cidades pelo nome para o autocomplete (até 5 resultados).</summary>
    /// <param name="query">Parte do nome da cidade, exemplo: São Pa</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidades encontradas, com nome em português quando houver; lista vazia sem resultado.</response>
    /// <response code="400">Parâmetro 'query' com menos de 2 ou mais de 100 caracteres.</response>
    /// <response code="503">OpenWeather indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<Location>>> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? string.Empty;

        if (trimmed.Length is < MinQueryLength or > MaxQueryLength)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Parâmetro 'query' deve ter entre 2 e 100 caracteres.");
        }

        var locations = await appService.SearchAsync(trimmed, cancellationToken);
        return Ok(locations);
    }
}
