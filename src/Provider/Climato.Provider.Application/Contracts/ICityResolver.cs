using Climato.Provider.Application.Model.Cities;

namespace Climato.Provider.Application.Contracts;

public interface ICityResolver
{
    Task<ResolvedCity?> ResolveAsync(string cityName, CancellationToken cancellationToken);
}
