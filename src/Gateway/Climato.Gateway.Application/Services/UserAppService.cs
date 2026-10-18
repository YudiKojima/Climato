using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository userRepository;

    public UserAppService(IUserRepository repository)
    {
        userRepository = repository;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(id, cancellationToken);

        if (user == null)
        {
            return false;
        }

        await userRepository.DeleteAsync(user, cancellationToken);
        return true;
    }
}
