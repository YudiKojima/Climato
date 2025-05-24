namespace WeatherGatewayApp.Domain.Cities
{
    public interface ICityRepository
    {
        Task<City> GetAsync(Guid id);
        Task<List<City>> GetAllAsync(Guid userId);
        Task<List<City>> GetAllFavoritesAsync(Guid userId);
        Task<City> GetByNameAsync(string cityName, Guid userId);
        Task CreateAsync(City city);
        Task UpdateAsync(City city);
        Task DeleteAsync(City city);
    }
}
