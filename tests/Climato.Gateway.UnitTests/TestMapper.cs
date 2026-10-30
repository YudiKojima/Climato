using Climato.Gateway.Application.Mappings;
using Mapster;
using MapsterMapper;

namespace Climato.Gateway.UnitTests;

internal static class TestMapper
{
    public static IMapper Create()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(CityMappings).Assembly);
        return new Mapper(config);
    }
}
