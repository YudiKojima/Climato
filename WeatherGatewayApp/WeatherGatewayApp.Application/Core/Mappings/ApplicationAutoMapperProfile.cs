using AutoMapper;
using WeatherGatewayApp.Application.Cities;
using WeatherGatewayApp.Domain.Cities;

namespace WeatherGatewayApp.Application.Core.Mappings
{
    public class ApplicationAutoMapperProfile : Profile
    {
        public ApplicationAutoMapperProfile() 
        {
            CreateMap<City, CityDto>();
        }
    }
}
