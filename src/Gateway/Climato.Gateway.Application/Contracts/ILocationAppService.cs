using Climato.Gateway.Application.Model.Locations;

namespace Climato.Gateway.Application.Contracts;

public interface ILocationAppService
{
    Task<IReadOnlyList<LocationDto>> SearchAsync(string query, CancellationToken cancellationToken);
}
