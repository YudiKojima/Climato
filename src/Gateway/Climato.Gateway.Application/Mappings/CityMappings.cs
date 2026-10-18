using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Domain.Cities;
using Mapster;

namespace Climato.Gateway.Application.Mappings;

public class CityMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<City, CityDto>();
    }
}
