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

        public async Task<List<City>> GetAllAsync()
        {
            return await context.City
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<City>> GetAllFavoritesAsync()
        {
            return await context.City
                .Where(c => c.IsFavorite)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<City?> GetByNameAsync(string cityName)
        {
            return await context.City
                .Where(c => c.Name == cityName)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(City city)
        {
            context.City.Remove(city);
            await context.SaveChangesAsync();
        }
    }
}
