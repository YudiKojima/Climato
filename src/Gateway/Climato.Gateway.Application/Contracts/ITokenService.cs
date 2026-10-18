using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.Application.Contracts;

public interface ITokenService
{
    AuthToken Create(User user);
}
