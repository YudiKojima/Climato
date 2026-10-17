using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Domain.Users;
using MapsterMapper;

namespace Climato.Gateway.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UserAppService(IUserRepository repository, IMapper mapper)
    {
        userRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(id, cancellationToken);

        if (user == null)
        {
            return false;
        }

        await userRepository.DeleteAsync(user, cancellationToken);
        return true;
    }

    public async Task<UserDto?> GetAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(email, cancellationToken);

        if (user == null || user.Password != password)
        {
            return null;
        }

        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> PostAsync(UserPostDto dto, CancellationToken cancellationToken)
    {
        var allowCreate = await userRepository.AllowCreateAsync(dto.Email, cancellationToken);

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

        await userRepository.CreateAsync(user, cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}
