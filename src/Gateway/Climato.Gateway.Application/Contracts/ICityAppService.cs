using Climato.Gateway.Application.Model.Cities;

namespace Climato.Gateway.Application.Contracts;

public interface ICityAppService
{
    Task<List<CityDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<CityDto>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken);
    Task<CityDto?> ToggleFavoriteAsync(Guid id, CancellationToken cancellationToken);
    void DeleteAsync(Guid id);
}
