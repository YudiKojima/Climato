using AutoMapper;
using Climato.Gateway.Application.Cities;
using Climato.Gateway.Application.Users;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.Application.Core.Mappings;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile() 
    {
        CreateMap<City, CityDto>();
        CreateMap<User, UserDto>();
        CreateMap<User, UserPostDto>();
    }
}
