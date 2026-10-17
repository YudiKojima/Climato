using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Domain.Cities;
using MapsterMapper;

namespace Climato.Gateway.Application.Services;

public class CityAppService : ICityAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IMapper mapper;

    public CityAppService(ICityRepository repository, IMapper mapper)
    {
        cityRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, cancellationToken);

        if (city == null)
        {
            return false;
        }

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

    public async Task<CityDto?> ToggleFavoriteAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, cancellationToken);

        if (city == null)
        {
            return null;
        }

        city.ToggleFavorite();
        await cityRepository.UpdateAsync(city, cancellationToken);

        return mapper.Map<CityDto>(city);
    }
}
