using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Domain.Cities;
using MapsterMapper;

namespace Climato.Gateway.Application.Services;

public class CityAppService : ICityAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IWeatherProviderClient weatherProviderClient;
    private readonly IMapper mapper;

    public CityAppService(ICityRepository repository, IWeatherProviderClient providerClient, IMapper mapper)
    {
        cityRepository = repository;
        weatherProviderClient = providerClient;
        this.mapper = mapper;
    }

    public async Task<(CityDto City, bool Created)?> AddAsync(CityPostDto dto, Guid userId, CancellationToken cancellationToken)
    {
        var weather = await weatherProviderClient.GetWeatherAsync(dto.Name.Trim(), false, cancellationToken);

        if (weather == null)
        {
            return null;
        }

        var existing = await cityRepository.GetByNameAsync(weather.City, userId, cancellationToken);

        if (existing != null)
        {
            return (mapper.Map<CityDto>(existing), false);
        }

        var city = new City(weather.City, weather.Country, userId);
        await cityRepository.CreateAsync(city, cancellationToken);

        return (mapper.Map<CityDto>(city), true);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, userId, cancellationToken);

        if (city == null)
        {
            return false;
        }

        city.EnsureCanBeDeleted();
        await cityRepository.DeleteAsync(city, cancellationToken);
        return true;
    }

    public async Task<List<CityDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cities = await cityRepository.GetAllAsync(userId, cancellationToken);
        return mapper.Map<List<CityDto>>(cities);
    }

    public async Task<List<CityDto>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cities = await cityRepository.GetAllFavoritesAsync(userId, cancellationToken);
        return mapper.Map<List<CityDto>>(cities);
    }

    public async Task<CityDto?> ToggleFavoriteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, userId, cancellationToken);

        if (city == null)
        {
            return null;
        }

        city.ToggleFavorite();
        await cityRepository.UpdateAsync(city, cancellationToken);

        return mapper.Map<CityDto>(city);
    }
}
