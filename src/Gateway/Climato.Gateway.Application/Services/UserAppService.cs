using AutoMapper;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Users;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository userRepository;
    private readonly IMapper autoMapper;

    public UserAppService(IUserRepository repository, IMapper mapper)
    {
        userRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        autoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async void DeleteAsync(Guid id)
    {
        var user = await userRepository.GetAsync(id) ?? throw new ArgumentException("Usuário não encontrada");
        await userRepository.DeleteAsync(user);
    }

    public async Task<UserDto?> GetAsync(string email, string password)
    {
        var user = await userRepository.GetAsync(email);
        
        if (user == null || user.Password != password)
        {
            return null;
        }

        return autoMapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> PostAsync(UserPostDto dto)
    {
        var allowCreate = await userRepository.AllowCreateAsync(dto.Email);

        if (!allowCreate)
        {
            return null;
        }

        var user = new User()
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Name = dto.Name,
            Password = dto.Password,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(user);
        return autoMapper.Map<UserDto>(user);
    }
}
