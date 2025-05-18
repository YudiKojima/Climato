using WeatherGatewayApp.Application.Cities;

namespace WeatherGatewayApp.Application.Contracts
{
    public interface ICityAppService
    {
        Task<List<CityDto>> GetAllAsync();
        Task<List<CityDto>> GetAllFavoritesAsync();
        Task<CityDto> ToggleFavoriteAsync(Guid id);
        void DeleteAsync(Guid id);
    }
}
