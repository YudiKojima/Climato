namespace Climato.Gateway.Domain.Cities;

public interface ICityRepository
{
    Task<City?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<List<City>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<City>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken);
    Task<City?> GetByNameAsync(string cityName, Guid userId, CancellationToken cancellationToken);
    Task CreateAsync(City city, CancellationToken cancellationToken);
    Task UpdateAsync(City city, CancellationToken cancellationToken);
    Task DeleteAsync(City city, CancellationToken cancellationToken);
}
