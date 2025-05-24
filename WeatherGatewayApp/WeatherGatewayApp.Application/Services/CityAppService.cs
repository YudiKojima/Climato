using AutoMapper;
using WeatherGatewayApp.Application.Cities;
using WeatherGatewayApp.Application.Contracts;
using WeatherGatewayApp.Domain.Cities;

namespace WeatherGatewayApp.Application.Services
{
    public class CityAppService : ICityAppService
    {
        private readonly ICityRepository cityRepository;
        private readonly IMapper autoMapper;

        public CityAppService(ICityRepository repository, IMapper mapper)
        {
            cityRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            autoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async void DeleteAsync(Guid id)
        {
            var city = await cityRepository.GetAsync(id) ?? throw new ArgumentException("Cidade não encontrada");
            await cityRepository.DeleteAsync(city);
        }

        public async Task<List<CityDto>> GetAllAsync(Guid userId)
        {
            var cities = await cityRepository.GetAllAsync(userId);
            return autoMapper.Map<List<CityDto>>(cities);
        }

        public async Task<List<CityDto>> GetAllFavoritesAsync(Guid userId)
        {
            var cities = await cityRepository.GetAllFavoritesAsync(userId);
            return autoMapper.Map<List<CityDto>>(cities);
        }

        public async Task<CityDto?> ToggleFavoriteAsync(Guid id)
        {
            var city = await cityRepository.GetAsync(id);

            if (city == null)
            {
                return null;
            }

            city.IsFavorite = !city.IsFavorite;
            await cityRepository.UpdateAsync(city);

            return autoMapper.Map<CityDto>(city);
        }
    }
}
