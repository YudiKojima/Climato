using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Climato.Gateway.UnitTests.Application.Services;

public class AuthAppServiceTests
{
    private const string Password = "senha-de-teste-123";

    private readonly IUserRepository repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher<User> passwordHasher = Substitute.For<IPasswordHasher<User>>();
    private readonly ITokenService tokenService = Substitute.For<ITokenService>();
    private readonly AuthAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public AuthAppServiceTests()
    {
        tokenService.Create(Arg.Any<User>()).Returns(new AuthToken("jwt-de-teste", DateTimeOffset.UtcNow.AddHours(1)));
        appService = new AuthAppService(repository, passwordHasher, tokenService, TestMapper.Create());
    }

    [Fact]
    public async Task RegisterAsync_EmailJaUsado_DevolveNullSemCriar()
    {
        repository.AllowCreateAsync("ana@teste.dev", Arg.Any<CancellationToken>()).Returns(false);

        var auth = await appService.RegisterAsync(new RegisterPostDto { Email = "  Ana@Teste.dev ", Name = "Ana", Password = Password }, cancellationToken);

        auth.ShouldBeNull();
        await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task RegisterAsync_DadosValidos_CriaComHashEDevolveToken()
    {
        repository.AllowCreateAsync("ana@teste.dev", Arg.Any<CancellationToken>()).Returns(true);
        passwordHasher.HashPassword(Arg.Any<User>(), Password).Returns("hash-de-teste");

        var auth = await appService.RegisterAsync(new RegisterPostDto { Email = "Ana@Teste.dev", Name = "Ana", Password = Password }, cancellationToken);

        auth.ShouldNotBeNull();
        auth.Token.ShouldBe("jwt-de-teste");
        auth.User.Email.ShouldBe("ana@teste.dev");
        await repository.Received(1).CreateAsync(
            Arg.Is<User>(user => user.Email == "ana@teste.dev" && user.PasswordHash == "hash-de-teste"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_UsuarioInexistente_DevolveNull()
    {
        repository.GetAsync("ana@teste.dev", Arg.Any<CancellationToken>()).Returns((User?)null);

        var auth = await appService.LoginAsync(new LoginPostDto { Email = "Ana@Teste.dev", Password = Password }, cancellationToken);

        auth.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_SenhaErrada_DevolveNullSemEmitirToken()
    {
        var user = UserWithHash("hash-de-teste");
        passwordHasher.VerifyHashedPassword(user, "hash-de-teste", "senha-errada").Returns(PasswordVerificationResult.Failed);

        var auth = await appService.LoginAsync(new LoginPostDto { Email = "ana@teste.dev", Password = "senha-errada" }, cancellationToken);

        auth.ShouldBeNull();
        tokenService.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task LoginAsync_HashAntigo_RefazOHashESalva()
    {
        var user = UserWithHash("hash-antigo");
        passwordHasher.VerifyHashedPassword(user, "hash-antigo", Password).Returns(PasswordVerificationResult.SuccessRehashNeeded);
        passwordHasher.HashPassword(user, Password).Returns("hash-novo");

        var auth = await appService.LoginAsync(new LoginPostDto { Email = "ana@teste.dev", Password = Password }, cancellationToken);

        auth.ShouldNotBeNull();
        user.PasswordHash.ShouldBe("hash-novo");
        await repository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_SenhaCorreta_DevolveTokenSemRegravar()
    {
        var user = UserWithHash("hash-de-teste");
        passwordHasher.VerifyHashedPassword(user, "hash-de-teste", Password).Returns(PasswordVerificationResult.Success);

        var auth = await appService.LoginAsync(new LoginPostDto { Email = "ana@teste.dev", Password = Password }, cancellationToken);

        auth.ShouldNotBeNull();
        auth.Token.ShouldBe("jwt-de-teste");
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, cancellationToken);
    }

    private User UserWithHash(string hash)
    {
        var user = new User("ana@teste.dev", "Ana");
        user.SetPasswordHash(hash);
        repository.GetAsync("ana@teste.dev", Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
