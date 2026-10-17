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
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        appService.DeleteAsync(id);
        return Ok();
    }
}
