namespace WeatherGatewayApp.Domain.Users
{
    public interface IUserRepository
    {
        Task<User> GetAsync(string email);
        Task<User> GetAsync(Guid id);
        Task CreateAsync(User user);
        Task DeleteAsync(User user);
        Task<bool> AllowCreateAsync(string email);
    }
}
