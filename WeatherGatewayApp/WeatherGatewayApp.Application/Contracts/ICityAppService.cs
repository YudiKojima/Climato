using WeatherGatewayApp.Application.Cities;

namespace WeatherGatewayApp.Application.Contracts
{
    public interface ICityAppService
    {
        Task<List<CityDto>> GetAllAsync(Guid userId);
        Task<List<CityDto>> GetAllFavoritesAsync(Guid userId);
        Task<CityDto?> ToggleFavoriteAsync(Guid id);
        void DeleteAsync(Guid id);
    }
}
