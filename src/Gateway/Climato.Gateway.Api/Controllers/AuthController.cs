using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService appService;

    public AuthController(IAuthAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Cria uma conta e devolve o token de acesso.</summary>
    /// <response code="201">Conta criada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="409">E-mail já cadastrado.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthDto>> Register(RegisterPostDto dto, CancellationToken cancellationToken)
    {
        var auth = await appService.RegisterAsync(dto, cancellationToken);

        if (auth == null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "E-mail já cadastrado.");
        }

        return StatusCode(StatusCodes.Status201Created, auth);
    }

    /// <summary>Autentica o usuário e devolve o token de acesso.</summary>
    /// <response code="200">Autenticado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">E-mail ou senha inválidos.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthDto>> Login(LoginPostDto dto, CancellationToken cancellationToken)
    {
        var auth = await appService.LoginAsync(dto, cancellationToken);

        if (auth == null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha inválidos.");
        }

        return Ok(auth);
    }
}
