using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.UnitTests.Application.Services;

public class UserAppServiceTests
{
    private readonly IUserRepository repository = Substitute.For<IUserRepository>();
    private readonly UserAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public UserAppServiceTests()
    {
        appService = new UserAppService(repository);
    }

    [Fact]
    public async Task DeleteAsync_UsuarioInexistente_DevolveFalse()
    {
        var userId = Guid.NewGuid();
        repository.GetAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var deleted = await appService.DeleteAsync(userId, cancellationToken);

        deleted.ShouldBeFalse();
        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_UsuarioExistente_RemoveEDevolveTrue()
    {
        var user = new User("ana@teste.dev", "Ana");
        repository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var deleted = await appService.DeleteAsync(user.Id, cancellationToken);

        deleted.ShouldBeTrue();
        await repository.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
    }
}
