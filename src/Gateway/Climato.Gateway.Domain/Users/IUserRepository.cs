namespace Climato.Gateway.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task CreateAsync(User user, CancellationToken cancellationToken);
    Task DeleteAsync(User user, CancellationToken cancellationToken);
    Task<bool> AllowCreateAsync(string email, CancellationToken cancellationToken);
}
