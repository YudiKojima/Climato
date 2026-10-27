# Fase 04 — Testes unitários: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** criar os projetos de testes unitários do Provider e do Gateway e cobrir o comportamento atual, conforme o [design aprovado](design.md). Testes de integração ficaram em backlog, com plano próprio em [`integracao-plan.md`](integracao-plan.md).

**Arquitetura:** primeiro a infraestrutura (runner, pacotes, projeto) com o primeiro teste do Provider, depois os AppServices do Provider, depois o Gateway (domínio, AppServices, validators, `GlobalExceptionHandler`), por fim documentação e fechamento da Fase 04. Cada tarefa termina com testes verdes e vira um commit.

**Stack:** xUnit v3 (Microsoft.Testing.Platform) · Shouldly · NSubstitute.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) antes de editar. O design vale mais que este plano em caso de conflito.
- **Nenhuma mudança em `src/`.** A fase caracteriza o comportamento atual. Se um teste deste plano falhar contra o código atual, **não corrigir o código**: parar, usar `superpowers:systematic-debugging` para confirmar se é bug real ou erro do teste; bug real → registrar como pendência no `project-overview.md` e avisar o usuário antes de seguir.
- Testes de caracterização nascem verdes (o código já existe): o ciclo de cada passo é *escrever → rodar → ver passar*. Para garantir que o teste não passa por acidente, em pelo menos um teste por classe trocar temporariamente o valor esperado, ver falhar e desfazer.
- Unitários não usam rede, Docker nem chave real; toda dependência é substituída com NSubstitute.
- **Commits:** um por tarefa (0–4), com as mensagens indicadas. A aprovação deste plano é a autorização explícita para esses commits (mesma exceção das Fases 01–03 à regra "nunca commitar durante a fase"). Antes de cada commit, `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/testes`, a partir do `origin/master` com o PR #11 (03b) mergeado.
- Código C# de teste segue o `backend.md`: chaves em todo `if`/`else`/`foreach`; campos privados `camelCase` sem `_`; namespace file-scoped igual à pasta; sem comentários do "o quê" (nem `// Arrange`); nomes `Metodo_Cenario_ResultadoEsperado` em português.
- Fora do escopo: testes de integração ([`integracao-plan.md`](integracao-plan.md), backlog), correções de bug (backlog), CI (05), cobertura de código, testes do front.

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| Pacotes `xunit.runner.visualstudio` e `Microsoft.NET.Test.Sdk` | Não usados; `global.json` opta pelo runner **Microsoft.Testing.Platform** (`"test": { "runner": "Microsoft.Testing.Platform" }`) e o comando passa a ser `dotnet test --solution Climato.slnx` | Spike: no SDK 10, o xUnit v3 4.x recusa o `dotnet test` em modo VSTest ("Testing with VSTest target is no longer supported") |

Um spike descartável (fora do repositório) confirmou xUnit v3 4.0.2 + MTP + Shouldly 4.3.0 + NSubstitute 6.2.0 no SDK 10.0.301.

## Mapa de arquivos

Caminhos relativos à raiz do repositório. Nenhum arquivo de `src/` muda.

| Arquivo | Ação | Tarefa |
|---|---|---|
| `docs/features/04-testes/{design,plan,integracao-plan}.md` | Criar (já escritos) | 0 |
| `global.json`, `Directory.Packages.props`, `Climato.slnx` | Modificar | 1 |
| `tests/Directory.Build.props` | Criar | 1 |
| `tests/Climato.Provider.UnitTests/Climato.Provider.UnitTests.csproj` | Criar | 1 |
| `tests/Climato.Provider.UnitTests/Application/Services/ProviderCacheTests.cs` | Criar | 1 |
| `tests/Climato.Provider.UnitTests/OpenWeatherFakes.cs` | Criar | 2 |
| `tests/Climato.Provider.UnitTests/Application/Services/{Forecast,Weather,Location}AppServiceTests.cs` | Criar | 2 |
| `tests/Climato.Gateway.UnitTests/Climato.Gateway.UnitTests.csproj` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/{TestMapper,TestData}.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Domain/{City,User}Tests.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Application/Services/{City,Weather,Auth,User,Location}AppServiceTests.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Application/Validators/*ValidatorTests.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Api/Errors/GlobalExceptionHandlerTests.cs` | Criar | 3 |
| `docs/architecture/backend.md`, `docs/runbook.md`, `CLAUDE.md` | Modificar | 4 |
| `docs/project-overview.md`, `docs/features/00-roteiro-portfolio/auditoria-inicial.md` | Modificar | 4 |

---

### Tarefa 0: Preparar a branch e versionar o design

- [x] **Passo 1:** branch `dev/testes` criada a partir do `origin/master` (`git log --oneline -1` mostra `Merge pull request #11 from YudiKojima/dev/novos-endpoints`), sem upstream.
- [x] **Passo 2:** commit dos documentos aprovados:

```bash
git add docs/features/04-testes/
git commit -m "Adiciona design e planos da Fase 04 (testes)" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 1: Infraestrutura de testes + primeiro teste (`ProviderCache`)

**Files:**
- Modify: `global.json`, `Directory.Packages.props`, `Climato.slnx`
- Create: `tests/Directory.Build.props`, `tests/Climato.Provider.UnitTests/Climato.Provider.UnitTests.csproj`, `tests/Climato.Provider.UnitTests/Application/Services/ProviderCacheTests.cs`

**Interfaces:**
- Produces: `tests/Directory.Build.props` (todo projeto em `tests/` herda xUnit v3 + Shouldly e os `global using` de `Xunit` e `Shouldly`); pacotes centralizados para as tarefas 2–3.

- [x] **Passo 1: runner de testes no `global.json`**

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- [x] **Passo 2: pacotes no `Directory.Packages.props`** — acrescentar, em ordem alfabética dentro do `ItemGroup` existente:

```xml
    <PackageVersion Include="NSubstitute" Version="6.2.0" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="xunit.v3" Version="4.0.2" />
```

- [x] **Passo 3: `tests/Directory.Build.props`**

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="Shouldly" />
  </ItemGroup>
</Project>
```

- [x] **Passo 4: `tests/Climato.Provider.UnitTests/Climato.Provider.UnitTests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="NSubstitute" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="NSubstitute" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Provider\Climato.Provider.Application\Climato.Provider.Application.csproj" />
  </ItemGroup>

</Project>
```

- [x] **Passo 5: incluir os projetos no `Climato.slnx`** — nova pasta depois de `/Provider/` (o `Climato.Gateway.UnitTests` entra na Tarefa 3, nesta mesma pasta):

```xml
  <Folder Name="/Tests/">
    <Project Path="tests/Climato.Provider.UnitTests/Climato.Provider.UnitTests.csproj" />
  </Folder>
```

- [x] **Passo 6: `ProviderCacheTests.cs`**

```csharp
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
```

- [x] **Passo 7: rodar**

Run: `dotnet test --project tests/Climato.Provider.UnitTests`
Expected: 8 testes, 0 falhas. Trocar `"clima:sao paulo"` por `"clima:são paulo"`, ver falhar, desfazer.

- [x] **Passo 8: build e suíte pela solução**

Run: `dotnet build Climato.slnx` → 0 erros, 0 avisos novos. `dotnet test --solution Climato.slnx` → mesmos 8 testes.

- [x] **Passo 9: `/revisar-padrao` e commit**

```bash
git add global.json Directory.Packages.props Climato.slnx tests/
git commit -m "Cria a infraestrutura de testes e cobre a chave do cache do Provider" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 2: Provider — AppServices (unitários)

**Files:**
- Create: `tests/Climato.Provider.UnitTests/OpenWeatherFakes.cs`, `Application/Services/ForecastAppServiceTests.cs`, `Application/Services/WeatherAppServiceTests.cs`, `Application/Services/LocationAppServiceTests.cs`

**Interfaces:**
- Consumes: projeto da Tarefa 1.
- Produces: `OpenWeatherFakes` (interno ao projeto de unitários do Provider).

- [x] **Passo 1: `OpenWeatherFakes.cs`**

```csharp
using Climato.Provider.Application.Model.OpenWeather;

namespace Climato.Provider.UnitTests;

internal static class OpenWeatherFakes
{
    public static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0)
    {
        return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
    }

    public static OpenWeatherCurrentResponse Current(
        string name,
        TimeSpan offset,
        DateTimeOffset sunriseUtc,
        DateTimeOffset sunsetUtc,
        double windSpeed = 5.0)
    {
        return new OpenWeatherCurrentResponse(
            name,
            (int)offset.TotalSeconds,
            new OpenWeatherSys("BR", sunriseUtc.ToUnixTimeSeconds(), sunsetUtc.ToUnixTimeSeconds()),
            [new OpenWeatherCondition("céu limpo", "01d")],
            new OpenWeatherMain(25.4, 26.1, 22.0, 28.3, 60, 1013),
            new OpenWeatherWind(windSpeed, 180));
    }

    public static OpenWeatherForecastItem Item(
        DateTimeOffset utc,
        double tempMin = 20,
        double tempMax = 25,
        double pop = 0,
        double windSpeed = 1,
        string description = "nublado",
        string icon = "04d")
    {
        return new OpenWeatherForecastItem(
            utc.ToUnixTimeSeconds(),
            new OpenWeatherForecastMain(tempMin, tempMax),
            [new OpenWeatherCondition(description, icon)],
            new OpenWeatherForecastWind(windSpeed),
            pop);
    }

    public static OpenWeatherForecastResponse Forecast(TimeSpan offset, params OpenWeatherForecastItem[] items)
    {
        return new OpenWeatherForecastResponse(items, new OpenWeatherForecastCity("Cidade", "XX", (int)offset.TotalSeconds));
    }
}
```

- [x] **Passo 2: `ForecastAppServiceTests.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Weathers;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class ForecastAppServiceTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly ForecastAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public ForecastAppServiceTests()
    {
        appService = new ForecastAppService(openWeatherClient, cache);
    }

    [Fact]
    public async Task GetByCityAsync_FusoNegativo_AgrupaPelaDataLocal()
    {
        ReturnsForecast(Forecast(TimeSpan.FromHours(-3),
            Item(Utc(2026, 10, 26, 2)),
            Item(Utc(2026, 10, 26, 15)),
            Item(Utc(2026, 10, 27, 2))));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.Select(day => day.Date).ShouldBe(new[] { new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 26) });
    }

    [Fact]
    public async Task GetByCityAsync_FusoPositivo_AgrupaPelaDataLocalEmOrdem()
    {
        ReturnsForecast(Forecast(TimeSpan.FromHours(9),
            Item(Utc(2026, 10, 26, 16)),
            Item(Utc(2026, 10, 26, 3)),
            Item(Utc(2026, 10, 25, 14))));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.Select(day => day.Date).ShouldBe(new[] { new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 27) });
    }

    [Fact]
    public async Task GetByCityAsync_PontosDoMesmoDia_AgregaMinMaxChuvaVentoECondicaoDoMeioDia()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero,
            Item(Utc(2026, 10, 26, 9), tempMin: 18, tempMax: 22, pop: 0.1, windSpeed: 2.0),
            Item(Utc(2026, 10, 26, 12), tempMin: 17, tempMax: 26, pop: 0.456, windSpeed: 5.5, description: "chuva leve", icon: "10d"),
            Item(Utc(2026, 10, 26, 15), tempMin: 19, tempMax: 24, pop: 0.3, windSpeed: 3.0)));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        var day = forecast.Days.ShouldHaveSingleItem();
        day.TempMin.ShouldBe(17);
        day.TempMax.ShouldBe(26);
        day.PrecipitationProbability.ShouldBe(46);
        day.WindSpeedKmh.ShouldBe(19.8);
        day.Description.ShouldBe("chuva leve");
        day.Icon.ShouldBe("10d");
    }

    [Fact]
    public async Task GetByCityAsync_SemPontoAoMeioDia_UsaACondicaoDoPontoMaisProximo()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero,
            Item(Utc(2026, 10, 26, 6), description: "madrugada"),
            Item(Utc(2026, 10, 26, 13), description: "tarde"),
            Item(Utc(2026, 10, 26, 21), description: "noite")));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.ShouldHaveSingleItem().Description.ShouldBe("tarde");
    }

    [Fact]
    public async Task GetByCityAsync_SeteDias_DevolveOsCincoPrimeiros()
    {
        var items = Enumerable.Range(0, 7)
            .Select(offset => Item(Utc(2026, 10, 26, 12).AddDays(offset)))
            .ToArray();
        ReturnsForecast(Forecast(TimeSpan.Zero, items));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.Days.Count.ShouldBe(5);
        forecast.Days[0].Date.ShouldBe(new DateOnly(2026, 10, 26));
        forecast.Days[4].Date.ShouldBe(new DateOnly(2026, 10, 30));
    }

    [Fact]
    public async Task GetByCityAsync_PrevisaoNoCache_NaoChamaAOpenWeather()
    {
        var cached = new Forecast { City = "São Paulo", Country = "BR", Days = [] };
        cache.GetAsync<Forecast>("previsao", "São Paulo", Arg.Any<CancellationToken>()).Returns(cached);

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldBeSameAs(cached);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetForecastAsync(default!, default);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeInexistente_DevolveNullSemGravarNoCache()
    {
        openWeatherClient.GetForecastAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((OpenWeatherForecastResponse?)null);

        var forecast = await appService.GetByCityAsync("Atlântida", cancellationToken);

        forecast.ShouldBeNull();
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Forecast>(default!, default!, default!, default, default);
    }

    [Fact]
    public async Task GetByCityAsync_Sucesso_GravaNoCacheDePrevisaoPorTrintaMinutos()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero, Item(Utc(2026, 10, 26, 12))));

        await appService.GetByCityAsync("São Paulo", cancellationToken);

        await cache.Received(1).SetAsync("previsao", "São Paulo", Arg.Any<Forecast>(), TimeSpan.FromMinutes(30), Arg.Any<CancellationToken>());
    }

    private void ReturnsForecast(OpenWeatherForecastResponse response)
    {
        openWeatherClient.GetForecastAsync("São Paulo", Arg.Any<CancellationToken>()).Returns(response);
    }
}
```

- [x] **Passo 3: `WeatherAppServiceTests.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Weathers;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly ICityImageAppService cityImageAppService = Substitute.For<ICityImageAppService>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly WeatherAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(openWeatherClient, cityImageAppService, cache);
    }

    [Fact]
    public async Task GetByCityAsync_RespostaDaOpenWeather_ConverteParaOContrato()
    {
        openWeatherClient.GetCurrentAsync("sao paulo", Arg.Any<CancellationToken>())
            .Returns(Current("São Paulo", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30), windSpeed: 5.0));
        cityImageAppService.GetImageUrlAsync("São Paulo", Arg.Any<CancellationToken>()).Returns("https://images.pexels.com/sp.jpg");

        var weather = await appService.GetByCityAsync("sao paulo", cancellationToken);

        weather.ShouldNotBeNull();
        weather.City.ShouldBe("São Paulo");
        weather.Country.ShouldBe("BR");
        weather.Description.ShouldBe("céu limpo");
        weather.WindSpeedKmh.ShouldBe(18.0);
        weather.Sunrise.Offset.ShouldBe(TimeSpan.FromHours(-3));
        weather.Sunrise.DateTime.ShouldBe(new DateTime(2026, 10, 26, 5, 45, 0));
        weather.Sunset.DateTime.ShouldBe(new DateTime(2026, 10, 26, 18, 30, 0));
        weather.ImageUrl.ShouldBe("https://images.pexels.com/sp.jpg");
    }

    [Fact]
    public async Task GetByCityAsync_PexelsSemImagem_DevolveClimaSemImagem()
    {
        openWeatherClient.GetCurrentAsync("Lima", Arg.Any<CancellationToken>())
            .Returns(Current("Lima", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));
        cityImageAppService.GetImageUrlAsync("Lima", Arg.Any<CancellationToken>()).Returns((string?)null);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldNotBeNull();
        weather.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCityAsync_ClimaNoCache_NaoChamaAOpenWeather()
    {
        var cached = new Weather
        {
            City = "Lima", Country = "PE", Description = "nublado", Icon = "04d",
            Temperature = 18, FeelsLike = 18, TempMin = 16, TempMax = 20, Humidity = 80, Pressure = 1012,
            WindSpeedKmh = 10, WindDirection = 180,
            Sunrise = Utc(2026, 10, 26, 10), Sunset = Utc(2026, 10, 26, 23)
        };
        cache.GetAsync<Weather>("clima", "Lima", Arg.Any<CancellationToken>()).Returns(cached);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldBeSameAs(cached);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetCurrentAsync(default!, default);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeInexistente_DevolveNullSemImagemESemCache()
    {
        openWeatherClient.GetCurrentAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((OpenWeatherCurrentResponse?)null);

        var weather = await appService.GetByCityAsync("Atlântida", cancellationToken);

        weather.ShouldBeNull();
        await cityImageAppService.DidNotReceiveWithAnyArgs().GetImageUrlAsync(default!, default);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Weather>(default!, default!, default!, default, default);
    }

    [Fact]
    public async Task GetByCityAsync_Sucesso_GravaNoCacheDeClimaPorDezMinutosComOTextoPesquisado()
    {
        openWeatherClient.GetCurrentAsync("sao paulo", Arg.Any<CancellationToken>())
            .Returns(Current("São Paulo", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30)));

        await appService.GetByCityAsync("sao paulo", cancellationToken);

        await cache.Received(1).SetAsync("clima", "sao paulo", Arg.Any<Weather>(), TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }
}
```

- [x] **Passo 4: `LocationAppServiceTests.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Locations;

namespace Climato.Provider.UnitTests.Application.Services;

public class LocationAppServiceTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly LocationAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public LocationAppServiceTests()
    {
        appService = new LocationAppService(openWeatherClient, cache);
    }

    [Fact]
    public async Task SearchAsync_ComNomeEmPortugues_UsaLocalNamesPt()
    {
        ReturnsLocations("Londres", new OpenWeatherLocationResponse("London", new Dictionary<string, string> { ["pt"] = "Londres", ["en"] = "London" }, "GB", "England"));

        var locations = await appService.SearchAsync("Londres", cancellationToken);

        var location = locations.ShouldHaveSingleItem();
        location.Name.ShouldBe("Londres");
        location.State.ShouldBe("England");
        location.Country.ShouldBe("GB");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchAsync_SemNomeEmPortugues_UsaName(bool hasLocalNames)
    {
        var localNames = hasLocalNames ? new Dictionary<string, string> { ["en"] = "Lima" } : null;
        ReturnsLocations("Lima", new OpenWeatherLocationResponse("Lima", localNames, "PE", null));

        var locations = await appService.SearchAsync("Lima", cancellationToken);

        locations.ShouldHaveSingleItem().Name.ShouldBe("Lima");
    }

    [Fact]
    public async Task SearchAsync_SemResultado_GravaListaVaziaNoCachePorVinteEQuatroHoras()
    {
        ReturnsLocations("Xyzw");

        var locations = await appService.SearchAsync("Xyzw", cancellationToken);

        locations.ShouldBeEmpty();
        await cache.Received(1).SetAsync("locais", "Xyzw", Arg.Is<List<Location>>(list => list.Count == 0), TimeSpan.FromHours(24), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_BuscaNoCache_NaoChamaAOpenWeather()
    {
        cache.GetAsync<List<Location>>("locais", "Lima", Arg.Any<CancellationToken>()).Returns([new Location { Name = "Lima", Country = "PE" }]);

        var locations = await appService.SearchAsync("Lima", cancellationToken);

        locations.ShouldHaveSingleItem().Name.ShouldBe("Lima");
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetLocationsAsync(default!, default);
    }

    private void ReturnsLocations(string query, params OpenWeatherLocationResponse[] locations)
    {
        openWeatherClient.GetLocationsAsync(query, Arg.Any<CancellationToken>()).Returns(locations);
    }
}
```

- [x] **Passo 5: rodar**

Run: `dotnet test --project tests/Climato.Provider.UnitTests`
Expected: 8 (Tarefa 1) + 8 + 5 + 5 = 26 testes, 0 falhas. Em cada classe nova, trocar um valor esperado, ver falhar, desfazer.

- [x] **Passo 6: `/revisar-padrao` e commit**

```bash
git add tests/Climato.Provider.UnitTests
git commit -m "Cobre os AppServices do Provider com testes unitários" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 3: Gateway — unitários

**Files:**
- Create: `tests/Climato.Gateway.UnitTests/Climato.Gateway.UnitTests.csproj`, `TestMapper.cs`, `TestData.cs`, `Domain/CityTests.cs`, `Domain/UserTests.cs`, `Application/Services/{CityAppServiceTests,WeatherAppServiceTests,AuthAppServiceTests,UserAppServiceTests,LocationAppServiceTests}.cs`, `Application/Validators/{RegisterPostDtoValidatorTests,LoginPostDtoValidatorTests,LocationSearchQueryValidatorTests}.cs`, `Api/Errors/GlobalExceptionHandlerTests.cs`
- Modify: `Climato.slnx`

**Interfaces:**
- Produces: `TestMapper.Create()` (`MapsterMapper.IMapper` com uma `TypeAdapterConfig` própria lendo os `IRegister` da Application); `TestData.Weather(string city)` (`WeatherDto` completo).

- [x] **Passo 1: `.csproj`** e linha no `Climato.slnx`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="NSubstitute" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="NSubstitute" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Gateway\Climato.Gateway.Api\Climato.Gateway.Api.csproj" />
  </ItemGroup>

</Project>
```

```xml
    <Project Path="tests/Climato.Gateway.UnitTests/Climato.Gateway.UnitTests.csproj" />
```

- [x] **Passo 2: `TestMapper.cs` e `TestData.cs`**

`TestMapper` usa uma instância própria de `TypeAdapterConfig` (não a `GlobalSettings`) para não compartilhar estado entre testes paralelos. Não é mapeamento de produção (BE-19 vale para `src/`).

```csharp
using Climato.Gateway.Application.Mappings;
using Mapster;
using MapsterMapper;

namespace Climato.Gateway.UnitTests;

internal static class TestMapper
{
    public static IMapper Create()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(CityMappings).Assembly);
        return new Mapper(config);
    }
}
```

```csharp
using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.UnitTests;

internal static class TestData
{
    public static WeatherDto Weather(string city, string country = "PE")
    {
        return new WeatherDto
        {
            City = city,
            Country = country,
            Description = "nublado",
            Icon = "04d",
            Temperature = 18,
            FeelsLike = 18,
            TempMin = 16,
            TempMax = 20,
            Humidity = 80,
            Pressure = 1012,
            WindSpeedKmh = 10,
            WindDirection = 180,
            Sunrise = new DateTimeOffset(2026, 10, 26, 5, 45, 0, TimeSpan.FromHours(-5)),
            Sunset = new DateTimeOffset(2026, 10, 26, 18, 10, 0, TimeSpan.FromHours(-5))
        };
    }
}
```

- [x] **Passo 3: `Domain/CityTests.cs` e `Domain/UserTests.cs`**

```csharp
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Exceptions;

namespace Climato.Gateway.UnitTests.Domain;

public class CityTests
{
    [Fact]
    public void Constructor_NovaCidade_NaoFavoritaComIdEDataDeCriacao()
    {
        var userId = Guid.NewGuid();

        var city = new City("Lima", "PE", userId);

        city.Id.ShouldNotBe(Guid.Empty);
        city.UserId.ShouldBe(userId);
        city.IsFavorite.ShouldBeFalse();
        city.CreatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ToggleFavorite_DuasVezes_VoltaAoEstadoInicial()
    {
        var city = new City("Lima", "PE", Guid.NewGuid());

        city.ToggleFavorite();
        city.IsFavorite.ShouldBeTrue();

        city.ToggleFavorite();
        city.IsFavorite.ShouldBeFalse();
    }

    [Fact]
    public void EnsureCanBeDeleted_CidadeFavorita_LancaDomainException()
    {
        var city = new City("Lima", "PE", Guid.NewGuid());
        city.ToggleFavorite();

        var exception = Should.Throw<DomainException>(() => city.EnsureCanBeDeleted());

        exception.Message.ShouldBe("Não é possível remover uma cidade favorita.");
    }

    [Fact]
    public void EnsureCanBeDeleted_CidadeNaoFavorita_NaoLanca()
    {
        var city = new City("Lima", "PE", Guid.NewGuid());

        Should.NotThrow(() => city.EnsureCanBeDeleted());
    }
}
```

```csharp
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.UnitTests.Domain;

public class UserTests
{
    [Theory]
    [InlineData("  Ana@Teste.DEV ", "ana@teste.dev")]
    [InlineData("ana@teste.dev", "ana@teste.dev")]
    public void NormalizeEmail_EmailComEspacosEMaiusculas_DevolveMinusculoSemEspacos(string email, string expected)
    {
        User.NormalizeEmail(email).ShouldBe(expected);
    }

    [Fact]
    public void Constructor_NovoUsuario_NormalizaEmailEAparaONome()
    {
        var user = new User(" Ana@Teste.dev ", "  Ana Souza ");

        user.Id.ShouldNotBe(Guid.Empty);
        user.Email.ShouldBe("ana@teste.dev");
        user.Name.ShouldBe("Ana Souza");
    }
}
```

- [x] **Passo 4: `Application/Services/CityAppServiceTests.cs`**

```csharp
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Exceptions;

namespace Climato.Gateway.UnitTests.Application.Services;

public class CityAppServiceTests
{
    private readonly ICityRepository repository = Substitute.For<ICityRepository>();
    private readonly CityAppService appService;
    private readonly Guid userId = Guid.NewGuid();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public CityAppServiceTests()
    {
        appService = new CityAppService(repository, TestMapper.Create());
    }

    [Fact]
    public async Task DeleteAsync_CidadeNaoEncontradaParaOUsuario_DevolveFalseSemRemover()
    {
        var cityId = Guid.NewGuid();
        repository.GetAsync(cityId, userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var deleted = await appService.DeleteAsync(cityId, userId, cancellationToken);

        deleted.ShouldBeFalse();
        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task DeleteAsync_CidadeFavorita_LancaDomainExceptionSemRemover()
    {
        var city = new City("Lima", "PE", userId);
        city.ToggleFavorite();
        repository.GetAsync(city.Id, userId, Arg.Any<CancellationToken>()).Returns(city);

        await Should.ThrowAsync<DomainException>(() => appService.DeleteAsync(city.Id, userId, cancellationToken));

        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task DeleteAsync_CidadeDoUsuario_RemoveEDevolveTrue()
    {
        var city = new City("Lima", "PE", userId);
        repository.GetAsync(city.Id, userId, Arg.Any<CancellationToken>()).Returns(city);

        var deleted = await appService.DeleteAsync(city.Id, userId, cancellationToken);

        deleted.ShouldBeTrue();
        await repository.Received(1).DeleteAsync(city, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleFavoriteAsync_CidadeNaoEncontradaParaOUsuario_DevolveNullSemAtualizar()
    {
        var cityId = Guid.NewGuid();
        repository.GetAsync(cityId, userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var result = await appService.ToggleFavoriteAsync(cityId, userId, cancellationToken);

        result.ShouldBeNull();
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_CidadeDoUsuario_AlternaSalvaEDevolveODto()
    {
        var city = new City("Lima", "PE", userId);
        repository.GetAsync(city.Id, userId, Arg.Any<CancellationToken>()).Returns(city);

        var result = await appService.ToggleFavoriteAsync(city.Id, userId, cancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(city.Id);
        result.Name.ShouldBe("Lima");
        result.IsFavorite.ShouldBeTrue();
        await repository.Received(1).UpdateAsync(city, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_CidadesDoUsuario_DevolveOsDtos()
    {
        repository.GetAllAsync(userId, Arg.Any<CancellationToken>()).Returns([new City("Lima", "PE", userId), new City("Quito", "EC", userId)]);

        var cities = await appService.GetAllAsync(userId, cancellationToken);

        cities.Select(city => city.Name).ShouldBe(new[] { "Lima", "Quito" });
    }
}
```

- [x] **Passo 5: `Application/Services/WeatherAppServiceTests.cs`**

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Cities;

namespace Climato.Gateway.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private readonly ICityRepository repository = Substitute.For<ICityRepository>();
    private readonly IWeatherProviderClient providerClient = Substitute.For<IWeatherProviderClient>();
    private readonly WeatherAppService appService;
    private readonly Guid userId = Guid.NewGuid();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(repository, providerClient);
    }

    [Fact]
    public async Task GetByCityAsync_ProviderNaoEncontra_DevolveNullSemTocarNoRepositorio()
    {
        providerClient.GetWeatherAsync("Atlantida", Arg.Any<CancellationToken>()).Returns((WeatherDto?)null);

        var weather = await appService.GetByCityAsync("Atlantida", userId, cancellationToken);

        weather.ShouldBeNull();
        repository.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByCityAsync_CidadeNova_CriaNoHistoricoComONomeDevolvidoPeloProvider()
    {
        providerClient.GetWeatherAsync("lima", Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima"));
        repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns((City?)null);

        var weather = await appService.GetByCityAsync("lima", userId, cancellationToken);

        weather.ShouldNotBeNull();
        await repository.Received(1).CreateAsync(
            Arg.Is<City>(city => city.Name == "Lima" && city.Country == "PE" && city.UserId == userId && !city.IsFavorite),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByCityAsync_CidadeJaNoHistorico_NaoCriaDeNovo()
    {
        providerClient.GetWeatherAsync("Lima", Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima"));
        repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns(new City("Lima", "PE", userId));

        await appService.GetByCityAsync("Lima", userId, cancellationToken);

        await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task GetForecastAsync_RepassaARespostaDoProvider()
    {
        var forecast = new ForecastDto { City = "Lima", Country = "PE", Days = [] };
        providerClient.GetForecastAsync("Lima", Arg.Any<CancellationToken>()).Returns(forecast);

        var result = await appService.GetForecastAsync("Lima", cancellationToken);

        result.ShouldBeSameAs(forecast);
    }
}
```

- [x] **Passo 6: `Application/Services/AuthAppServiceTests.cs`**

```csharp
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
        await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
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
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    private User UserWithHash(string hash)
    {
        var user = new User("ana@teste.dev", "Ana");
        user.SetPasswordHash(hash);
        repository.GetAsync("ana@teste.dev", Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
```

- [x] **Passo 7: `Application/Services/UserAppServiceTests.cs` e `LocationAppServiceTests.cs`**

```csharp
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
        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
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
```

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Services;

namespace Climato.Gateway.UnitTests.Application.Services;

public class LocationAppServiceTests
{
    [Fact]
    public async Task SearchAsync_QueryComEspacos_RepassaAoProviderAparada()
    {
        var providerClient = Substitute.For<IWeatherProviderClient>();
        IReadOnlyList<LocationDto> expected = [new LocationDto { Name = "Lima", Country = "PE" }];
        providerClient.SearchLocationsAsync("Lima", Arg.Any<CancellationToken>()).Returns(expected);
        var appService = new LocationAppService(providerClient);

        var locations = await appService.SearchAsync("  Lima ", TestContext.Current.CancellationToken);

        locations.ShouldBeSameAs(expected);
    }
}
```

- [x] **Passo 8: validators** (`Application/Validators/`)

```csharp
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class RegisterPostDtoValidatorTests
{
    private readonly RegisterPostDtoValidator validator = new();

    [Fact]
    public void Validate_DadosValidos_SemErros()
    {
        var result = validator.Validate(new RegisterPostDto { Email = "ana@teste.dev", Name = "Ana", Password = "senha-segura" });

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Ana", "senha-segura", "Informe o e-mail.")]
    [InlineData("nao-e-email", "Ana", "senha-segura", "E-mail inválido.")]
    [InlineData("ana@teste.dev", "", "senha-segura", "Informe o nome.")]
    [InlineData("ana@teste.dev", "Ana", "", "Informe a senha.")]
    [InlineData("ana@teste.dev", "Ana", "curta", "A senha deve ter entre 8 e 128 caracteres.")]
    public void Validate_DadoInvalido_DevolveMensagemEmPortugues(string email, string name, string password, string message)
    {
        var result = validator.Validate(new RegisterPostDto { Email = email, Name = name, Password = password });

        result.Errors.Select(error => error.ErrorMessage).ShouldContain(message);
    }
}
```

```csharp
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class LoginPostDtoValidatorTests
{
    private readonly LoginPostDtoValidator validator = new();

    [Theory]
    [InlineData("", "senha", "Informe o e-mail.")]
    [InlineData("ana@teste.dev", "", "Informe a senha.")]
    public void Validate_CampoVazio_DevolveMensagemEmPortugues(string email, string password, string message)
    {
        var result = validator.Validate(new LoginPostDto { Email = email, Password = password });

        result.Errors.Select(error => error.ErrorMessage).ShouldContain(message);
    }

    [Fact]
    public void Validate_DadosPreenchidos_SemErros()
    {
        validator.Validate(new LoginPostDto { Email = "ana@teste.dev", Password = "qualquer" }).IsValid.ShouldBeTrue();
    }
}
```

```csharp
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class LocationSearchQueryValidatorTests
{
    private const string Message = "Parâmetro 'query' deve ter entre 2 e 100 caracteres.";

    private readonly LocationSearchQueryValidator validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  a  ")]
    public void Validate_QueryAusenteOuCurta_DevolveMensagem(string? query)
    {
        var result = validator.Validate(new LocationSearchQuery { Query = query });

        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(Message);
    }

    [Fact]
    public void Validate_QueryLonga_DevolveMensagem()
    {
        var result = validator.Validate(new LocationSearchQuery { Query = new string('a', 101) });

        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(Message);
    }

    [Theory]
    [InlineData("Li")]
    [InlineData("  Lima  ")]
    public void Validate_QueryValida_SemErros(string query)
    {
        validator.Validate(new LocationSearchQuery { Query = query }).IsValid.ShouldBeTrue();
    }
}
```

- [x] **Passo 9: `Api/Errors/GlobalExceptionHandlerTests.cs`**

```csharp
using Climato.Gateway.Api.Errors;
using Climato.Gateway.Application.Exceptions;
using Climato.Gateway.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Climato.Gateway.UnitTests.Api.Errors;

public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService problemDetailsService = Substitute.For<IProblemDetailsService>();
    private readonly GlobalExceptionHandler handler;
    private readonly DefaultHttpContext httpContext = new();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public GlobalExceptionHandlerTests()
    {
        problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(true);
        handler = new GlobalExceptionHandler(problemDetailsService, NullLogger<GlobalExceptionHandler>.Instance);
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_Devolve409ComAMensagem()
    {
        var handled = await handler.TryHandleAsync(httpContext, new DomainException("Regra violada."), cancellationToken);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Status == StatusCodes.Status409Conflict
            && context.ProblemDetails.Title == "Operação não permitida."
            && context.ProblemDetails.Detail == "Regra violada."));
    }

    [Fact]
    public async Task TryHandleAsync_UserNotFoundException_Devolve401SessaoInvalida()
    {
        await handler.TryHandleAsync(httpContext, new UserNotFoundException(), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Title == "Sessão inválida."));
    }

    [Fact]
    public async Task TryHandleAsync_ProviderUnavailableException_Devolve503()
    {
        await handler.TryHandleAsync(httpContext, new ProviderUnavailableException("O Provider respondeu 500."), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Title == "Serviço de clima indisponível. Tente novamente em instantes."
            && context.ProblemDetails.Detail == null));
    }

    [Fact]
    public async Task TryHandleAsync_ExcecaoInesperada_Devolve500SemDetalhe()
    {
        await handler.TryHandleAsync(httpContext, new InvalidOperationException("detalhe interno"), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(context =>
            context.ProblemDetails.Title == "Erro interno."
            && context.ProblemDetails.Detail == null));
    }

    [Fact]
    public async Task TryHandleAsync_CanceladoPeloCliente_Devolve499SemCorpo()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        httpContext.RequestAborted = aborted.Token;

        var handled = await handler.TryHandleAsync(httpContext, new OperationCanceledException(), cancellationToken);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
        await problemDetailsService.DidNotReceiveWithAnyArgs().TryWriteAsync(default!);
    }

    [Fact]
    public async Task TryHandleAsync_CancelamentoSemAbortoDoCliente_Devolve500()
    {
        await handler.TryHandleAsync(httpContext, new OperationCanceledException(), cancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}
```

- [x] **Passo 10: rodar**

Run: `dotnet test --project tests/Climato.Gateway.UnitTests`
Expected: 4 + 3 + 6 + 4 + 6 + 2 + 1 + 6 + 3 + 6 + 6 = 47 testes, 0 falhas. Em cada classe, trocar um valor esperado, ver falhar, desfazer.

- [x] **Passo 11: `/revisar-padrao` e commit**

```bash
git add Climato.slnx tests/Climato.Gateway.UnitTests
git commit -m "Cobre domínio, AppServices, validators e erros do Gateway com testes unitários" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 4: Documentação e fechamento da Fase 04

**Files:**
- Modify: `docs/architecture/backend.md`, `CLAUDE.md`, `docs/runbook.md`, `docs/project-overview.md`, `docs/features/00-roteiro-portfolio/auditoria-inicial.md`

- [ ] **Passo 1: `backend.md`**
  - Stack: linha **Testes** com `xUnit v3 (Microsoft.Testing.Platform), Shouldly, NSubstitute` na coluna **Atual** e `Testcontainers, WireMock.Net (integração, backlog)` em **Alvo**.
  - Estrutura da Solução: trocar "Testes em `tests/` (Fase 04)." por:

```
tests/
├── Climato.Provider.UnitTests/  ← AppServices e cache do Provider
└── Climato.Gateway.UnitTests/   ← domínio, AppServices, validators e GlobalExceptionHandler
```

  - Nova seção `### Testes` em **Convenções de Código**, depois de **Health checks**:

```markdown
### Testes
- **TDD é regra a partir da Fase 04:** regra, AppService, entidade ou endpoint novo/alterado nasce com teste em `tests/` (BE-18); correção de bug começa com um teste que reproduz o bug.
- Um projeto de unitários por serviço (`Climato.{Gateway,Provider}.UnitTests`); pastas espelham `src/`. `tests/Directory.Build.props` traz xUnit v3 e Shouldly; versões no `Directory.Packages.props`.
- Nome `Metodo_Cenario_ResultadoEsperado` em português; corpo Arrange/Act/Assert separado por linha em branco, sem comentários de seção. `TestContext.Current.CancellationToken` como token.
- Dependências com NSubstitute; sem rede, Docker ou chave real. Métodos privados testados pelo comportamento público (sem `InternalsVisibleTo`). Mapper de teste com `TypeAdapterConfig` própria (`TestMapper`).
- Testes de integração (`WebApplicationFactory` + Testcontainers + WireMock) estão em backlog: [`integracao-plan.md`](../features/04-testes/integracao-plan.md).
```

  - Verificação: sem mudança (já aponta para o `CLAUDE.md`).

- [ ] **Passo 2: `CLAUDE.md`**
  - Verificação:

```bash
dotnet build Climato.slnx
dotnet test --solution Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

  - Frase abaixo do bloco: "A Fase 05 (`docker compose`) atualiza esta seção e o [`runbook.md`](docs/runbook.md)."
  - Tabela de skills, linha `test-driven-development`: "**Regra** desde a Fase 04: toda regra nova nasce com teste; bug começa com teste que o reproduz".

- [ ] **Passo 3: `runbook.md`** — nova seção `## 5. Testes` (renumerar as seguintes se houver):

````markdown
## 5. Testes

```bash
dotnet test --solution Climato.slnx
```

Roda os testes unitários de `tests/` (sem banco, Docker ou chaves). O `global.json` usa o runner Microsoft.Testing.Platform; por isso o comando é `dotnet test --solution`/`--project` (o modo VSTest não é suportado pelo xUnit v3 no SDK 10).
````

- [ ] **Passo 4: `auditoria-inicial.md`** — linha A24, coluna Status: `✅ Fase 04 (unitários; integração em backlog)`.

- [ ] **Passo 5: `project-overview.md`**
  - Estado Atual: data e "(após a Fase 04)"; em **Funciona**, acrescentar "testes unitários nos dois serviços"; em **Não há**, trocar "testes" por "testes de integração"; na **Stack**, acrescentar `xUnit v3 · Shouldly · NSubstitute`.
  - Fase 04:

```markdown
#### Fase 04 — Testes ✅ Concluída (AAAA-MM-DD)
Reduzida a testes unitários para acelerar o roteiro: dois projetos (`Climato.{Gateway,Provider}.UnitTests`) cobrindo domínio, AppServices, cache, validators e mapeamento de erros. Integração (Testcontainers + WireMock) em backlog com plano pronto.
Design: [`design.md`](features/04-testes/design.md) · Plano: [`plan.md`](features/04-testes/plan.md) · Integração (backlog): [`integracao-plan.md`](features/04-testes/integracao-plan.md)
Auditoria: A24.
```

  - Pendências: trocar o rótulo `[Fase 04]` por `[Backlog]` em **todos** os itens que o têm (Mapster, resposta incompleta do clima, 400 do Provider para `cityName` em branco, `ValidationProblemDetails` duplicado, ramo 499, índice único em `city`, resposta incompleta da previsão) e acrescentar:

```markdown
- [Backlog] Testes de integração (WebApplicationFactory + Testcontainers Postgres/Redis + WireMock), plano em [`integracao-plan.md`](features/04-testes/integracao-plan.md); ao executar, incluir no CI da Fase 05 (Docker no runner)
```

- [ ] **Passo 6: verificação completa**

Run, na raiz:
- `dotnet build Climato.slnx` → 0 erros, 0 avisos novos
- `dotnet test --solution Climato.slnx` → 26 + 47 = 73 testes, 0 falhas
- `npm --prefix web run lint` e `npm --prefix web run build` → sem erros (nada mudou no web)
- `git status` → nenhum arquivo em `src/` alterado.

- [ ] **Passo 7: `/revisar-padrao` e commit**

```bash
git add CLAUDE.md docs/
git commit -m "Documenta os testes unitários e fecha a Fase 04" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Passo 8: revisão e PR** — `superpowers:requesting-code-review`; com o usuário de acordo, `superpowers:finishing-a-development-branch` → PR `dev/testes` → `master` citando **A24**, o escopo reduzido (integração em backlog) e que nenhum arquivo de `src/` mudou.
