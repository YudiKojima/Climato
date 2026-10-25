using Climato.Provider.Domain.Locations;

namespace Climato.Provider.Application.Contracts;

public interface ILocationAppService
{
    Task<IReadOnlyList<Location>> SearchAsync(string query, CancellationToken cancellationToken);
}
