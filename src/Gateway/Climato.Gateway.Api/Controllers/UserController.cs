using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Users;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IUserAppService appService;

    public UserController(IUserAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Cria um usuário.</summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Post(UserPostDto dto, CancellationToken cancellationToken)
    {
        var user = await appService.PostAsync(dto, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>Busca o usuário por e-mail e senha.</summary>
    [HttpGet]
    public async Task<ActionResult<UserDto>> Get([FromQuery] UserGetDto dto, CancellationToken cancellationToken)
    {
        var user = await appService.GetAsync(dto.Email, dto.Password, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>Remove o usuário informado.</summary>
    /// <param name="id">Id do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Usuário removido.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
