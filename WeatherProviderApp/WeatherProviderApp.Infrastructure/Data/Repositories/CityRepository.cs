using Microsoft.EntityFrameworkCore;
using WeatherProviderApp.Domain.Cities;

namespace WeatherProviderApp.Infrastructure.Data.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly ConnectionContext context = new ConnectionContext();

        public async Task<List<City>> GetAllAsync()
        {
            return await context.City.ToListAsync();
        }
    }
}
