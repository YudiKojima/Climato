using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Locations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace Climato.Provider.UnitTests.Application.Services;

public class ProviderCacheTests
{
    private readonly IDistributedCache distributedCache = Substitute.For<IDistributedCache>();
    private readonly ProviderCache cache;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public ProviderCacheTests()
    {
        cache = new ProviderCache(distributedCache, NullLogger<ProviderCache>.Instance);
    }

    [Theory]
    [InlineData("clima", "  São   Paulo ", "clima:sao paulo")]
    [InlineData("previsao", "São Paulo", "previsao:sao paulo")]
    [InlineData("locais", "ZÜRICH", "locais:zurich")]
    public async Task GetAsync_TextoComAcentosEspacosEMaiusculas_UsaChaveNormalizada(string prefix, string text, string expectedKey)
    {
        await cache.GetAsync<Location>(prefix, text, cancellationToken);

        await distributedCache.Received(1).GetAsync(expectedKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_Clima_GravaComChaveEstavelETtlAbsoluto()
    {
        await cache.SetAsync("clima", "Florianópolis", NewLocation(), TimeSpan.FromMinutes(10), cancellationToken);

        await distributedCache.Received(1).SetAsync(
            "clima:florianopolis",
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(options => options.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(10)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_ValorGravado_DevolveOMesmoConteudo()
    {
        var memoryCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var realCache = new ProviderCache(memoryCache, NullLogger<ProviderCache>.Instance);

        await realCache.SetAsync("locais", "Lima", NewLocation(), TimeSpan.FromHours(24), cancellationToken);
        var result = await realCache.GetAsync<Location>("locais", "LIMA", cancellationToken);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Lima");
        result.Country.ShouldBe("PE");
    }

    [Fact]
    public async Task GetAsync_CacheFalha_DevolveNull()
    {
        distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Redis fora"));

        var result = await cache.GetAsync<Location>("clima", "Lima", cancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_CacheFalha_NaoPropagaAExcecao()
    {
        distributedCache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Redis fora"));

        await Should.NotThrowAsync(() => cache.SetAsync("clima", "Lima", NewLocation(), TimeSpan.FromMinutes(10), cancellationToken));
    }

    [Fact]
    public async Task GetAsync_Cancelado_PropagaOCancelamento()
    {
        distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => cache.GetAsync<Location>("clima", "Lima", cancellationToken));
    }

    private static Location NewLocation()
    {
        return new Location { Name = "Lima", Country = "PE" };
    }
}
