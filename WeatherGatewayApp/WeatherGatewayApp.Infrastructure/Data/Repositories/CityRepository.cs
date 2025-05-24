using Microsoft.EntityFrameworkCore;
using WeatherGatewayApp.Domain.Cities;

namespace WeatherGatewayApp.Infrastructure.Data.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly ConnectionContext context = new ConnectionContext();

        public async Task<City?> GetAsync(Guid id)
        {
            return await context.City
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(City city)
        {
            await context.City.AddAsync(city);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(City city)
        {
            context.City.Update(city);
            await context.SaveChangesAsync();
        }

        public async Task<List<City>> GetAllAsync(Guid userId)
        {
            return await context.City
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<City>> GetAllFavoritesAsync(Guid userId)
        {
            return await context.City
                .Where(c => c.UserId == userId && c.IsFavorite)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<City?> GetByNameAsync(string cityName, Guid userId)
        {
            return await context.City
                .Where(c => c.Name == cityName && c.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(City city)
        {
            context.City.Remove(city);
            await context.SaveChangesAsync();
        }
    }
}
