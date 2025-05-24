using Microsoft.EntityFrameworkCore;
using WeatherGatewayApp.Domain.Users;

namespace WeatherGatewayApp.Infrastructure.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ConnectionContext context = new ConnectionContext();

        public async Task CreateAsync(User user)
        {
            await context.User.AddAsync(user);
            await context.SaveChangesAsync();
        }

        public async Task<User> GetAsync(string email)
        {
            return await context.User
                .Where(c => c.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<User> GetAsync(Guid id)
        {
            return await context.User
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(User user)
        {
            context.User.Remove(user);
            await context.SaveChangesAsync();
        }

        public async Task<bool> AllowCreateAsync(string email)
        {
            var exists = await context.User.AnyAsync(c => c.Email == email);
            return !exists;
        }
    }
}
