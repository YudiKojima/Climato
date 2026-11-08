using System.Security.Cryptography;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Application.Options;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Climato.Gateway.Application.Services;

public class AuthAppService : IAuthAppService
{
    private readonly IUserRepository userRepository;
    private readonly ICityRepository cityRepository;
    private readonly IPasswordHasher<User> passwordHasher;
    private readonly ITokenService tokenService;
    private readonly IMapper mapper;
    private readonly IOptions<DemoOptions> demoOptions;

    public AuthAppService(
        IUserRepository userRepository,
        ICityRepository cityRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IMapper mapper,
        IOptions<DemoOptions> demoOptions)
    {
        this.userRepository = userRepository;
        this.cityRepository = cityRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
        this.mapper = mapper;
        this.demoOptions = demoOptions;
    }

    public async Task<AuthDto?> RegisterAsync(RegisterPostDto dto, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(dto.Email);

        if (email == User.NormalizeEmail(demoOptions.Value.Email))
        {
            return null;
        }

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

    public async Task<AuthDto?> LoginAsDemoAsync(CancellationToken cancellationToken)
    {
        var demo = demoOptions.Value;

        if (!demo.Enabled)
        {
            return null;
        }

        var user = await userRepository.GetAsync(User.NormalizeEmail(demo.Email), cancellationToken)
            ?? await CreateDemoUserAsync(demo, cancellationToken);

        return CreateAuth(user);
    }

    private async Task<User> CreateDemoUserAsync(DemoOptions demo, CancellationToken cancellationToken)
    {
        var user = new User(demo.Email, demo.Name);
        var unusablePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.SetPasswordHash(passwordHasher.HashPassword(user, unusablePassword));
        await userRepository.CreateAsync(user, cancellationToken);

        foreach (var city in demo.Cities)
        {
            await cityRepository.CreateAsync(new City(city.Name, city.Country, user.Id), cancellationToken);
        }

        return user;
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
