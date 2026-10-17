using Climato.Gateway.Application.Users;

namespace Climato.Gateway.Application.Contracts
{
    public interface IUserAppService
    {
        Task<UserDto?> PostAsync(UserPostDto dto);
        Task<UserDto?> GetAsync(string email, string password);
        void DeleteAsync(Guid id);
    }
}
