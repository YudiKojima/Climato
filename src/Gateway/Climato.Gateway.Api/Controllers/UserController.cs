using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserAppService appService;

    public UserController(IUserAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Remove a conta do usuário autenticado e o histórico de cidades dele.</summary>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Conta removida.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(User.GetUserId(), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
