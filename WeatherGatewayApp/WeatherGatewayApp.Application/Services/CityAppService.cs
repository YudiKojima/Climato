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
            var city = await cityRepository.GetAsync(id) ?? throw new Exception("Cidade não encontrada");
            await cityRepository.DeleteAsync(city);
        }

        public async Task<List<CityDto>> GetAllAsync()
        {
            var cities = await cityRepository.GetAllAsync();
            return autoMapper.Map<List<CityDto>>(cities);
        }

        public async Task<List<CityDto>> GetAllFavoritesAsync()
        {
            var cities = await cityRepository.GetAllFavoritesAsync();
            return autoMapper.Map<List<CityDto>>(cities);
        }

        public async Task<CityDto> ToggleFavoriteAsync(Guid id)
        {
            var city = await cityRepository.GetAsync(id) ?? throw new Exception("Cidade não encontrada");

            city.IsFavorite = !city.IsFavorite;
            await cityRepository.UpdateAsync(city);

            return autoMapper.Map<CityDto>(city);
        }
    }
}
