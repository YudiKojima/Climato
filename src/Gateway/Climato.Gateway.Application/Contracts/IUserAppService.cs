using Climato.Gateway.Application.Model.Users;

namespace Climato.Gateway.Application.Contracts;

public interface IUserAppService
{
    Task<UserDto?> PostAsync(UserPostDto dto, CancellationToken cancellationToken);
    Task<UserDto?> GetAsync(string email, string password, CancellationToken cancellationToken);
    void DeleteAsync(Guid id);
}
