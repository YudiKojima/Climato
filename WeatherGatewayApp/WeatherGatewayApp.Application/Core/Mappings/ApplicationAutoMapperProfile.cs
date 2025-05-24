using AutoMapper;
using WeatherGatewayApp.Application.Cities;
using WeatherGatewayApp.Application.Users;
using WeatherGatewayApp.Domain.Cities;
using WeatherGatewayApp.Domain.Users;

namespace WeatherGatewayApp.Application.Core.Mappings
{
    public class ApplicationAutoMapperProfile : Profile
    {
        public ApplicationAutoMapperProfile() 
        {
            CreateMap<City, CityDto>();
            CreateMap<User, UserDto>();
            CreateMap<User, UserPostDto>();
        }
    }
}
