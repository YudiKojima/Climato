using Climato.Gateway.Domain.Cities;
using Microsoft.EntityFrameworkCore;

namespace Climato.Gateway.Infrastructure.Data.Repositories;

public class CityRepository : ICityRepository
{
    private readonly ClimatoDbContext context;

    public CityRepository(ClimatoDbContext context)
    {
        this.context = context;
    }

    public async Task<City?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.Id == id && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(City city, CancellationToken cancellationToken)
    {
        await context.Cities.AddAsync(city, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(City city, CancellationToken cancellationToken)
    {
        context.Cities.Update(city);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<City>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<City>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.UserId == userId && c.IsFavorite)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<City?> GetByNameAsync(string cityName, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.Name == cityName && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAsync(City city, CancellationToken cancellationToken)
    {
        context.Cities.Remove(city);
        await context.SaveChangesAsync(cancellationToken);
    }
}
