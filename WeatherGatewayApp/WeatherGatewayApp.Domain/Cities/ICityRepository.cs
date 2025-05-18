namespace WeatherGatewayApp.Domain.Cities
{
    public interface ICityRepository
    {
        Task<City> GetAsync(Guid id);
        Task<List<City>> GetAllAsync();
        Task<List<City>> GetAllFavoritesAsync();
        Task<City> GetByNameAsync(string cityName);
        Task CreateAsync(City city);
        Task UpdateAsync(City city);
        Task DeleteAsync(City city);
    }
}
