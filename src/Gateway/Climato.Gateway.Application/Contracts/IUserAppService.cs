namespace Climato.Gateway.Application.Contracts;

public interface IUserAppService
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
