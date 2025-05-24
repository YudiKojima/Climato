using WeatherGatewayApp.Application.Users;

namespace WeatherGatewayApp.Application.Contracts
{
    public interface IUserAppService
    {
        Task<UserDto?> PostAsync(UserPostDto dto);
        Task<UserDto?> GetAsync(string email, string password);
        void DeleteAsync(Guid id);
    }
}
