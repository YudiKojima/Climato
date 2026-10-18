using Climato.Gateway.Application.Model.Auth;

namespace Climato.Gateway.Application.Contracts;

public interface IAuthAppService
{
    Task<AuthDto?> RegisterAsync(RegisterPostDto dto, CancellationToken cancellationToken);
    Task<AuthDto?> LoginAsync(LoginPostDto dto, CancellationToken cancellationToken);
}
