using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Domain.Users;
using Mapster;

namespace Climato.Gateway.Application.Mappings;

public class UserMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>();
    }
}
