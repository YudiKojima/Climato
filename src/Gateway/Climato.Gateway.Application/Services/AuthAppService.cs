using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Domain.Users;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;

namespace Climato.Gateway.Application.Services;

public class AuthAppService : IAuthAppService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher<User> passwordHasher;
    private readonly ITokenService tokenService;
    private readonly IMapper mapper;

    public AuthAppService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, ITokenService tokenService, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
        this.mapper = mapper;
    }

    public async Task<AuthDto?> RegisterAsync(RegisterPostDto dto, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(dto.Email);
        var allowCreate = await userRepository.AllowCreateAsync(email, cancellationToken);

        if (!allowCreate)
        {
            return null;
        }

        var user = new User(email, dto.Name);
        user.SetPasswordHash(passwordHasher.HashPassword(user, dto.Password));

        await userRepository.CreateAsync(user, cancellationToken);
        return CreateAuth(user);
    }

    public async Task<AuthDto?> LoginAsync(LoginPostDto dto, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(User.NormalizeEmail(dto.Email), cancellationToken);

        if (user == null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, dto.Password));
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        return CreateAuth(user);
    }

    private AuthDto CreateAuth(User user)
    {
        var token = tokenService.Create(user);

        return new AuthDto
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = mapper.Map<UserDto>(user)
        };
    }
}
