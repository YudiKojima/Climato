# Fase 07b — Busca e previsão: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** busca de cidades na Home, previsão de 5 dias com gráfico no detalhe, temperatura nos cards e nome da cidade em português em todo o fluxo — conforme o [design aprovado](busca-previsao-design.md).

**Arquitetura:** primeiro o Provider (resolver de cidade no geocoding, clima e previsão por coordenadas), com TDD nos testes unitários existentes; depois a lógica do front (serviços, utilitários e hooks, com Vitest); depois as três peças de tela (busca, temperatura nos cards, previsão com Recharts); por fim documentação e fechamento da Fase 07. Cada tarefa termina com a verificação verde e vira um commit.

**Stack:** .NET 10 · xUnit v3 · Shouldly · NSubstitute · React 19 · Ant Design 5.29 · Recharts 3 · Vitest.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) antes das Tarefas 0–1 e [`frontend.md`](../../architecture/frontend.md) antes das 2–5. O design vale mais que este plano em caso de conflito.
- **Gateway sem mudança de código.** Contratos `/weathers`, `/forecasts`, `/locations`, `/cities` iguais.
- TDD: teste primeiro, ver falhar, implementar, ver passar (BE-18, FE-12).
- C# segue o `backend.md`: chaves em todo `if`; campos `camelCase` sem `_`; namespace file-scoped; sem comentários do "o quê"; testes `Metodo_Cenario_ResultadoEsperado` em português.
- Front: cores só de token (`var(--ant-*)` no CSS, `theme.useToken()` nas props do Recharts); `style` inline só com valor dinâmico; feedback via `App.useApp()`/`useApiFeedback`.
- Rodar o web com `npm --prefix web run dev` (5173) e as APIs com `dotnet run` (runbook §4). **Não** subir o `web` do compose na 8080 (ocupada por outro projeto).
- **Commits:** um por tarefa (0–6), com as mensagens indicadas; a aprovação deste plano autoriza esses commits. Antes de cada commit, `/revisar-padrao`; com 🔴, parar. Mensagens em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/busca-previsao` (já criada a partir do `origin/master` com a 07a, sem upstream).
- Verificação ao fim de cada tarefa:

```bash
dotnet build Climato.slnx
dotnet test --solution Climato.slnx
npm --prefix web run lint
npm --prefix web test
npm --prefix web run build
```

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| — | Timeouts do client da OpenWeather passam de 5 s/15 s para **4 s/9 s** (tentativa/total) | O clima agora faz geocoding + clima em sequência: 15 + 15 + Pexels 5 = 35 s > tentativa de 25 s do Gateway (regra de timeouts escalonados do `backend.md`). Com 9 + 9 + 5 = 23 s < 25 s |
| Regra "nome em português" num lugar só | Propriedade calculada `PortugueseName` no `OpenWeatherLocationResponse` | Usada pelo `CityResolver` e pelo `LocationAppService` sem helper novo |
| `ResolvedCity` | `record` em `Application/Model/Cities/` | Não é entidade de domínio; só transporta o resultado do geocoding e vai para o cache |
| — | Teste do `OpenWeatherClient` com `HttpMessageHandler` falso garantindo `lat=-23.55` em cultura pt-BR | Vírgula decimal na URL quebraria todas as consultas em servidor com cultura pt-BR |
| Ícone da OpenWeather montado em cada tela | `utils/weatherIconUrl.js` | Usado no detalhe, nos cards e na previsão (FE-09) |
| Temperatura dentro do `CityCard` | Componente `CityTemperature` | Mantém o card focado em ações; skeleton e erro isolados |
| — | Na busca, o primeiro Enter busca; com resultados na tela e o mesmo texto, Enter abre o item destacado | O `Input.Search` dispara `onSearch` no Enter; sem isso, setas + Enter buscariam de novo |

---

### Tarefa 0: `CityResolver` no Provider

**Arquivos:**
- Modificar: `src/Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherLocationResponse.cs`, `src/Provider/Climato.Provider.Application/Services/LocationAppService.cs`, `tests/Climato.Provider.UnitTests/OpenWeatherFakes.cs`, `tests/Climato.Provider.UnitTests/Application/Services/LocationAppServiceTests.cs`
- Criar: `src/Provider/Climato.Provider.Application/Model/Cities/ResolvedCity.cs`, `src/Provider/Climato.Provider.Application/Contracts/ICityResolver.cs`, `src/Provider/Climato.Provider.Application/Services/CityResolver.cs`, `tests/Climato.Provider.UnitTests/Application/Services/CityResolverTests.cs`

**Interfaces:**
- Produz:
  - `OpenWeatherLocationResponse(string Name, IReadOnlyDictionary<string,string>? LocalNames, string Country, string? State, double Lat, double Lon)` com `string PortugueseName { get; }`.
  - `record ResolvedCity(string Name, string OriginalName, string Country, double Lat, double Lon)`.
  - `ICityResolver.ResolveAsync(string cityName, CancellationToken) → Task<ResolvedCity?>`; cache `cidades`, 24 h, vazio não cacheado.
  - `OpenWeatherFakes.Location(string name, string? portugueseName = null, string country = "XX", string? state = null, double lat = 0, double lon = 0)`.

- [ ] **Passo 1: Fake de geocoding em `tests/Climato.Provider.UnitTests/OpenWeatherFakes.cs`** — novo método no fim da classe:

```csharp
    public static OpenWeatherLocationResponse Location(
        string name,
        string? portugueseName = null,
        string country = "XX",
        string? state = null,
        double lat = 0,
        double lon = 0)
    {
        var localNames = portugueseName == null
            ? null
            : new Dictionary<string, string> { ["pt"] = portugueseName, ["en"] = name };

        return new OpenWeatherLocationResponse(name, localNames, country, state, lat, lon);
    }
```

- [ ] **Passo 2: Escrever `tests/Climato.Provider.UnitTests/Application/Services/CityResolverTests.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class CityResolverTests
{
    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly CityResolver resolver;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public CityResolverTests()
    {
        resolver = new CityResolver(openWeatherClient, cache);
    }

    [Fact]
    public async Task ResolveAsync_ComNomeEmPortugues_DevolveNomePtNomeOriginalECoordenadas()
    {
        ReturnsLocations("Tóquio", Location("Tokyo", "Tóquio", "JP", lat: 35.68, lon: 139.76));

        var city = await resolver.ResolveAsync("Tóquio", cancellationToken);

        city.ShouldBe(new ResolvedCity("Tóquio", "Tokyo", "JP", 35.68, 139.76));
    }

    [Fact]
    public async Task ResolveAsync_SemNomeEmPortugues_UsaONomeOriginal()
    {
        ReturnsLocations("Lima", Location("Lima", country: "PE"));

        var city = await resolver.ResolveAsync("Lima", cancellationToken);

        city.ShouldNotBeNull();
        city.Name.ShouldBe("Lima");
        city.OriginalName.ShouldBe("Lima");
    }

    [Fact]
    public async Task ResolveAsync_VariosResultados_UsaOPrimeiro()
    {
        ReturnsLocations("Springfield",
            Location("Springfield", country: "US", state: "Illinois", lat: 39.8),
            Location("Springfield", country: "US", state: "Missouri", lat: 37.2));

        var city = await resolver.ResolveAsync("Springfield", cancellationToken);

        city.ShouldNotBeNull();
        city.Lat.ShouldBe(39.8);
    }

    [Fact]
    public async Task ResolveAsync_SemResultado_DevolveNullSemGravarNoCache()
    {
        ReturnsLocations("Atlântida");

        var city = await resolver.ResolveAsync("Atlântida", cancellationToken);

        city.ShouldBeNull();
        await cache.DidNotReceiveWithAnyArgs().SetAsync<ResolvedCity>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_CidadeNoCache_NaoChamaAOpenWeather()
    {
        var cached = new ResolvedCity("Lima", "Lima", "PE", -12.04, -77.03);
        cache.GetAsync<ResolvedCity>("cidades", "Lima", Arg.Any<CancellationToken>()).Returns(cached);

        var city = await resolver.ResolveAsync("Lima", cancellationToken);

        city.ShouldBeSameAs(cached);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetLocationsAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_Sucesso_GravaNoCacheDeCidadesPorVinteEQuatroHoras()
    {
        ReturnsLocations("Lima", Location("Lima", country: "PE"));

        await resolver.ResolveAsync("Lima", cancellationToken);

        await cache.Received(1).SetAsync("cidades", "Lima", Arg.Any<ResolvedCity>(), TimeSpan.FromHours(24), Arg.Any<CancellationToken>());
    }

    private void ReturnsLocations(string query, params OpenWeatherLocationResponse[] locations)
    {
        openWeatherClient.GetLocationsAsync(query, Arg.Any<CancellationToken>()).Returns(locations);
    }
}
```

- [ ] **Passo 3: Rodar e ver falhar**

Run: `dotnet test --solution Climato.slnx`
Esperado: erro de compilação — `CityResolver`, `ResolvedCity` não existem e `OpenWeatherLocationResponse` não tem 6 parâmetros.

- [ ] **Passo 4: `OpenWeatherLocationResponse.cs`**

```csharp
namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherLocationResponse(
    string Name,
    IReadOnlyDictionary<string, string>? LocalNames,
    string Country,
    string? State,
    double Lat,
    double Lon)
{
    private const string PortugueseLanguageCode = "pt";

    public string PortugueseName => LocalNames?.GetValueOrDefault(PortugueseLanguageCode) ?? Name;
}
```

- [ ] **Passo 5: `Model/Cities/ResolvedCity.cs`**

```csharp
namespace Climato.Provider.Application.Model.Cities;

public sealed record ResolvedCity(string Name, string OriginalName, string Country, double Lat, double Lon);
```

- [ ] **Passo 6: `Contracts/ICityResolver.cs`**

```csharp
using Climato.Provider.Application.Model.Cities;

namespace Climato.Provider.Application.Contracts;

public interface ICityResolver
{
    Task<ResolvedCity?> ResolveAsync(string cityName, CancellationToken cancellationToken);
}
```

- [ ] **Passo 7: `Services/CityResolver.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;

namespace Climato.Provider.Application.Services;

public sealed class CityResolver : ICityResolver
{
    private const string CachePrefix = "cidades";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(24);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly IProviderCache cache;

    public CityResolver(IOpenWeatherClient openWeatherClient, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cache = cache;
    }

    public async Task<ResolvedCity?> ResolveAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<ResolvedCity>(CachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var locations = await openWeatherClient.GetLocationsAsync(cityName, cancellationToken);
        var first = locations.FirstOrDefault();

        if (first == null)
        {
            return null;
        }

        var city = new ResolvedCity(first.PortugueseName, first.Name, first.Country, first.Lat, first.Lon);

        await cache.SetAsync(CachePrefix, cityName, city, cacheDuration, cancellationToken);
        return city;
    }
}
```

- [ ] **Passo 8: `LocationAppService.cs` usa a regra compartilhada** — remover a constante `PortugueseLanguageCode` e trocar o `ToLocation`:

```csharp
    private static Location ToLocation(OpenWeatherLocationResponse location)
    {
        return new Location
        {
            Name = location.PortugueseName,
            State = location.State,
            Country = location.Country
        };
    }
```

- [ ] **Passo 9: `LocationAppServiceTests.cs` usa o fake novo** — trocar as duas construções diretas:

```csharp
        ReturnsLocations("Londres", Location("London", "Londres", "GB", "England"));
```

e, no teste `SearchAsync_SemNomeEmPortugues_UsaName`, manter o `[Theory]` construindo o record com os 6 parâmetros:

```csharp
        ReturnsLocations("Lima", new OpenWeatherLocationResponse("Lima", localNames, "PE", null, -12.04, -77.03));
```

Acrescentar `using static Climato.Provider.UnitTests.OpenWeatherFakes;` no topo.

- [ ] **Passo 10: Registrar no DI** — em `src/Provider/Climato.Provider.Application/DependencyInjection.cs`, antes de `services.AddScoped<IWeatherAppService, WeatherAppService>();`:

```csharp
        services.AddScoped<ICityResolver, CityResolver>();
```

- [ ] **Passo 11: Rodar e ver passar**

Run: `dotnet test --solution Climato.slnx`
Esperado: todos aprovados (91 + 6 do resolver = 97).

- [ ] **Passo 12: Verificação completa** e **commit**

```bash
git add src/Provider tests/Climato.Provider.UnitTests
git commit -m "Adiciona o CityResolver no Provider com nome em português e coordenadas"
```

---

### Tarefa 1: Clima e previsão por coordenadas

**Arquivos:**
- Modificar: `src/Provider/Climato.Provider.Application/Contracts/IOpenWeatherClient.cs`, `Services/OpenWeatherClient.cs`, `Services/WeatherAppService.cs`, `Services/ForecastAppService.cs`, `DependencyInjection.cs`; `tests/Climato.Provider.UnitTests/Application/Services/WeatherAppServiceTests.cs`, `ForecastAppServiceTests.cs`
- Criar: `tests/Climato.Provider.UnitTests/Application/Services/OpenWeatherClientTests.cs`

**Interfaces:**
- Consome: `ICityResolver`, `ResolvedCity` (Tarefa 0).
- Produz: `IOpenWeatherClient.GetCurrentAsync(double lat, double lon, CancellationToken)` e `GetForecastAsync(double lat, double lon, CancellationToken)`; `WeatherAppService(IOpenWeatherClient, ICityResolver, ICityImageAppService, IProviderCache)`; `ForecastAppService(IOpenWeatherClient, ICityResolver, IProviderCache)`.

- [ ] **Passo 1: Reescrever `WeatherAppServiceTests.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Services;
using Climato.Provider.Domain.Weathers;
using static Climato.Provider.UnitTests.OpenWeatherFakes;

namespace Climato.Provider.UnitTests.Application.Services;

public class WeatherAppServiceTests
{
    private static readonly ResolvedCity saoPaulo = new("São Paulo", "Sao Paulo", "BR", -23.55, -46.63);
    private static readonly ResolvedCity lima = new("Lima", "Lima", "PE", -12.04, -77.03);

    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly ICityResolver cityResolver = Substitute.For<ICityResolver>();
    private readonly ICityImageAppService cityImageAppService = Substitute.For<ICityImageAppService>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly WeatherAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public WeatherAppServiceTests()
    {
        appService = new WeatherAppService(openWeatherClient, cityResolver, cityImageAppService, cache);
    }

    [Fact]
    public async Task GetByCityAsync_RespostaDaOpenWeather_ConverteParaOContrato()
    {
        Resolves("sao paulo", saoPaulo);
        openWeatherClient.GetCurrentAsync(-23.55, -46.63, Arg.Any<CancellationToken>())
            .Returns(Current("Sé", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30), windSpeed: 5.0));
        cityImageAppService.GetImageUrlAsync("Sao Paulo", Arg.Any<CancellationToken>()).Returns("https://images.pexels.com/sp.jpg");

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
    public async Task GetByCityAsync_CidadeResolvida_UsaNomeEPaisDoGeocoding()
    {
        Resolves("Lima", lima);
        openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>())
            .Returns(Current("Miraflores", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldNotBeNull();
        weather.City.ShouldBe("Lima");
        weather.Country.ShouldBe("PE");
    }

    [Fact]
    public async Task GetByCityAsync_PexelsSemImagem_DevolveClimaSemImagem()
    {
        Resolves("Lima", lima);
        openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>())
            .Returns(Current("Lima", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));
        cityImageAppService.GetImageUrlAsync("Lima", Arg.Any<CancellationToken>()).Returns((string?)null);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldNotBeNull();
        weather.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCityAsync_ClimaNoCache_NaoResolveNemChamaAOpenWeather()
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
        await cityResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, cancellationToken);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetCurrentAsync(default, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeNaoResolvida_DevolveNullSemConsultarClimaNemGravarCache()
    {
        cityResolver.ResolveAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((ResolvedCity?)null);

        var weather = await appService.GetByCityAsync("Atlântida", cancellationToken);

        weather.ShouldBeNull();
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetCurrentAsync(default, default, cancellationToken);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Weather>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_ClimaNaoEncontrado_DevolveNullSemImagemESemCache()
    {
        Resolves("Lima", lima);
        openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>()).Returns((OpenWeatherCurrentResponse?)null);

        var weather = await appService.GetByCityAsync("Lima", cancellationToken);

        weather.ShouldBeNull();
        await cityImageAppService.DidNotReceiveWithAnyArgs().GetImageUrlAsync(default!, cancellationToken);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Weather>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_Sucesso_GravaNoCacheDeClimaPorDezMinutosComOTextoPesquisado()
    {
        Resolves("sao paulo", saoPaulo);
        openWeatherClient.GetCurrentAsync(-23.55, -46.63, Arg.Any<CancellationToken>())
            .Returns(Current("Sé", TimeSpan.FromHours(-3), Utc(2026, 10, 26, 8, 45), Utc(2026, 10, 26, 21, 30)));

        await appService.GetByCityAsync("sao paulo", cancellationToken);

        await cache.Received(1).SetAsync("clima", "sao paulo", Arg.Any<Weather>(), TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
    }

    private void Resolves(string cityName, ResolvedCity city)
    {
        cityResolver.ResolveAsync(cityName, Arg.Any<CancellationToken>()).Returns(city);
    }
}
```

- [ ] **Passo 2: Ajustar `ForecastAppServiceTests.cs`**
  - Campos e construtor:

```csharp
    private static readonly ResolvedCity saoPaulo = new("São Paulo", "Sao Paulo", "BR", -23.55, -46.63);

    private readonly IOpenWeatherClient openWeatherClient = Substitute.For<IOpenWeatherClient>();
    private readonly ICityResolver cityResolver = Substitute.For<ICityResolver>();
    private readonly IProviderCache cache = Substitute.For<IProviderCache>();
    private readonly ForecastAppService appService;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public ForecastAppServiceTests()
    {
        appService = new ForecastAppService(openWeatherClient, cityResolver, cache);
    }
```

  - `using Climato.Provider.Application.Model.Cities;` no topo.
  - O helper `ReturnsForecast` passa a resolver e consultar por coordenadas:

```csharp
    private void ReturnsForecast(OpenWeatherForecastResponse response)
    {
        cityResolver.ResolveAsync("São Paulo", Arg.Any<CancellationToken>()).Returns(saoPaulo);
        openWeatherClient.GetForecastAsync(-23.55, -46.63, Arg.Any<CancellationToken>()).Returns(response);
    }
```

  - `GetByCityAsync_PrevisaoNoCache_NaoChamaAOpenWeather`: trocar a última linha por

```csharp
        await cityResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, cancellationToken);
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetForecastAsync(default, default, cancellationToken);
```

  - Substituir `GetByCityAsync_CidadeInexistente_DevolveNullSemGravarNoCache` por:

```csharp
    [Fact]
    public async Task GetByCityAsync_CidadeNaoResolvida_DevolveNullSemGravarNoCache()
    {
        cityResolver.ResolveAsync("Atlântida", Arg.Any<CancellationToken>()).Returns((ResolvedCity?)null);

        var forecast = await appService.GetByCityAsync("Atlântida", cancellationToken);

        forecast.ShouldBeNull();
        await openWeatherClient.DidNotReceiveWithAnyArgs().GetForecastAsync(default, default, cancellationToken);
        await cache.DidNotReceiveWithAnyArgs().SetAsync<Forecast>(default!, default!, default!, default, cancellationToken);
    }

    [Fact]
    public async Task GetByCityAsync_CidadeResolvida_UsaNomeEPaisDoGeocoding()
    {
        ReturnsForecast(Forecast(TimeSpan.Zero, Item(Utc(2026, 10, 26, 12))));

        var forecast = await appService.GetByCityAsync("São Paulo", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.City.ShouldBe("São Paulo");
        forecast.Country.ShouldBe("BR");
    }
```

- [ ] **Passo 3: Escrever `OpenWeatherClientTests.cs`**

```csharp
using System.Globalization;
using System.Net;
using Climato.Provider.Application.Options;
using Climato.Provider.Application.Services;
using Microsoft.Extensions.Options;

namespace Climato.Provider.UnitTests.Application.Services;

public class OpenWeatherClientTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetCurrentAsync_CulturaPtBr_EnviaCoordenadasComPonto()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        try
        {
            await client.GetCurrentAsync(-23.55, -46.63, cancellationToken);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.Query.ShouldContain("lat=-23.55&lon=-46.63");
        handler.RequestUri.AbsolutePath.ShouldBe("/data/2.5/weather");
    }

    [Fact]
    public async Task GetForecastAsync_Coordenadas_ConsultaAPrevisaoPorLatLon()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);

        await client.GetForecastAsync(35.68, 139.76, cancellationToken);

        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.AbsolutePath.ShouldBe("/data/2.5/forecast");
        handler.RequestUri.Query.ShouldContain("lat=35.68&lon=139.76");
    }

    private static OpenWeatherClient CreateClient(HttpMessageHandler handler)
    {
        var options = Options.Create(new OpenWeatherOptions { BaseUrl = "https://openweather.test/", ApiKey = "chave-de-teste" });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.Value.BaseUrl) };
        return new OpenWeatherClient(httpClient, options);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
```

O 404 faz o client devolver `null`, então o teste não precisa de JSON.

- [ ] **Passo 4: Rodar e ver falhar**

Run: `dotnet test --solution Climato.slnx`
Esperado: erro de compilação — `GetCurrentAsync(double, double, …)` e os construtores novos não existem.

- [ ] **Passo 5: `IOpenWeatherClient.cs`**

```csharp
using Climato.Provider.Application.Model.OpenWeather;

namespace Climato.Provider.Application.Contracts;

public interface IOpenWeatherClient
{
    Task<OpenWeatherCurrentResponse?> GetCurrentAsync(double lat, double lon, CancellationToken cancellationToken);
    Task<OpenWeatherForecastResponse?> GetForecastAsync(double lat, double lon, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpenWeatherLocationResponse>> GetLocationsAsync(string query, CancellationToken cancellationToken);
}
```

- [ ] **Passo 6: `OpenWeatherClient.cs`** — `using System.Globalization;` e os dois métodos:

```csharp
    public Task<OpenWeatherCurrentResponse?> GetCurrentAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?{Coordinates(lat, lon)}&appid={apiKey}&units=metric&lang=pt_br";
        return GetAsync<OpenWeatherCurrentResponse>(url, cancellationToken);
    }

    public Task<OpenWeatherForecastResponse?> GetForecastAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/forecast?{Coordinates(lat, lon)}&appid={apiKey}&units=metric&lang=pt_br";
        return GetAsync<OpenWeatherForecastResponse>(url, cancellationToken);
    }
```

e o helper privado, junto do `GetAsync<T>`:

```csharp
    private static string Coordinates(double lat, double lon)
    {
        return string.Create(CultureInfo.InvariantCulture, $"lat={lat}&lon={lon}");
    }
```

- [ ] **Passo 7: `WeatherAppService.cs`**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Cities;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Services;

public sealed class WeatherAppService : IWeatherAppService
{
    private const string CachePrefix = "clima";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(10);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly ICityResolver cityResolver;
    private readonly ICityImageAppService cityImageAppService;
    private readonly IProviderCache cache;

    public WeatherAppService(
        IOpenWeatherClient openWeatherClient,
        ICityResolver cityResolver,
        ICityImageAppService cityImageAppService,
        IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cityResolver = cityResolver;
        this.cityImageAppService = cityImageAppService;
        this.cache = cache;
    }

    public async Task<Weather?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<Weather>(CachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var city = await cityResolver.ResolveAsync(cityName, cancellationToken);

        if (city == null)
        {
            return null;
        }

        var current = await openWeatherClient.GetCurrentAsync(city.Lat, city.Lon, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = await cityImageAppService.GetImageUrlAsync(city.OriginalName, cancellationToken);
        var weather = ToWeather(city, current, imageUrl);

        await cache.SetAsync(CachePrefix, cityName, weather, cacheDuration, cancellationToken);
        return weather;
    }

    private static Weather ToWeather(ResolvedCity city, OpenWeatherCurrentResponse current, string? imageUrl)
    {
        var offset = TimeSpan.FromSeconds(current.Timezone);
        var condition = current.Weather[0];

        return new Weather
        {
            City = city.Name,
            Country = city.Country,
            Description = condition.Description,
            Icon = condition.Icon,
            Temperature = current.Main.Temp,
            FeelsLike = current.Main.FeelsLike,
            TempMin = current.Main.TempMin,
            TempMax = current.Main.TempMax,
            Humidity = current.Main.Humidity,
            Pressure = current.Main.Pressure,
            WindSpeedKmh = Math.Round(current.Wind.Speed * 3.6, 1),
            WindDirection = current.Wind.Deg,
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(current.Sys.Sunrise).ToOffset(offset),
            Sunset = DateTimeOffset.FromUnixTimeSeconds(current.Sys.Sunset).ToOffset(offset),
            ImageUrl = imageUrl
        };
    }
}
```

- [ ] **Passo 8: `ForecastAppService.cs`**
  - `using Climato.Provider.Application.Model.Cities;`; campo `private readonly ICityResolver cityResolver;`; construtor `ForecastAppService(IOpenWeatherClient openWeatherClient, ICityResolver cityResolver, IProviderCache cache)` atribuindo os três.
  - `GetByCityAsync`, depois do bloco do cache:

```csharp
        var city = await cityResolver.ResolveAsync(cityName, cancellationToken);

        if (city == null)
        {
            return null;
        }

        var response = await openWeatherClient.GetForecastAsync(city.Lat, city.Lon, cancellationToken);

        if (response == null)
        {
            return null;
        }

        var forecast = ToForecast(city, response);
```

  - `ToForecast(ResolvedCity city, OpenWeatherForecastResponse response)` com `City = city.Name, Country = city.Country` (o resto igual).

- [ ] **Passo 9: Timeouts da OpenWeather** — em `DependencyInjection.cs` do Provider, no `AddHttpClient<IOpenWeatherClient, OpenWeatherClient>`:

```csharp
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(9);
                options.Retry.MaxRetryAttempts = 2;
            });
```

- [ ] **Passo 10: Rodar e ver passar**

Run: `dotnet test --solution Climato.slnx`
Esperado: todos aprovados (97 + 2 do client + 2 de clima + 1 de previsão = 102).

- [ ] **Passo 11: Conferência manual** (Provider em `dotnet run`, Scalar em `https://localhost:7002/scalar` ou `curl -k`)
  - `GET /weathers?cityName=Tóquio` → `"city": "Tóquio"`, `"country": "JP"`, foto de Tóquio.
  - `GET /forecasts?cityName=Londres` → `"city": "Londres"`.
  - `GET /weathers?cityName=Xyzabc` → 404.
  - Conferir os nomes de rota/parâmetro reais no Scalar antes (o Provider usa query `cityName`).

- [ ] **Passo 12: Verificação completa** e **commit**

```bash
git add src/Provider tests/Climato.Provider.UnitTests
git commit -m "Consulta clima e previsão por coordenadas da cidade resolvida, com nome em português"
```

---

### Tarefa 2: Serviços, utilitários e hooks do front

**Arquivos:**
- Criar: `web/src/services/locations.js`, `locations.test.js`; `web/src/utils/formatWeekday.js`, `formatWeekday.test.js`, `weatherIconUrl.js`, `weatherIconUrl.test.js`; `web/src/hooks/useForecast.js`, `useForecast.test.js`, `useCitySearch.js`, `useCitySearch.test.jsx`
- Modificar: `web/src/services/weather.js`, `web/src/pages/WeatherDetails.jsx` (usar `weatherIconUrl`)

**Interfaces:**
- Produz:
  - `searchLocations(query) → Promise<Array<{ name, state, country }>>`.
  - `getForecastByCity(cityName) → Promise<{ city, country, days: Array<{ date, description, icon, tempMin, tempMax, precipitationProbability, windSpeedKmh }> }>`.
  - `formatWeekday("2026-11-06") → "sex., 06/11"`; `weatherIconUrl("01d") → "https://openweathermap.org/img/wn/01d@2x.png"`.
  - `useForecast(cityName) → { data, isLoading, error, reload }`.
  - `useCitySearch() → { query, results, isSearching, search(value), clear() }` (`query` = último termo buscado; `""` sem busca).

- [ ] **Passo 1: Testes de serviço e utilitários**

`web/src/services/locations.test.js`:

```js
import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { searchLocations } from "./locations";

vi.mock("./client", () => ({ default: { get: vi.fn() } }));

describe("searchLocations", () => {
  it("consulta /locations com o termo em query", async () => {
    client.get.mockResolvedValue({ data: [{ name: "Tóquio", state: null, country: "JP" }] });

    const result = await searchLocations("Tóquio");

    expect(client.get).toHaveBeenCalledWith("/locations", { params: { query: "Tóquio" } });
    expect(result).toEqual([{ name: "Tóquio", state: null, country: "JP" }]);
  });
});
```

`web/src/utils/formatWeekday.test.js`:

```js
import { describe, expect, it } from "vitest";
import { formatWeekday } from "./formatWeekday";

describe("formatWeekday", () => {
  it("mostra dia da semana e data em pt-BR", () => {
    expect(formatWeekday("2026-11-06")).toBe("sex., 06/11");
  });

  it("não pula de dia por causa do fuso", () => {
    expect(formatWeekday("2026-01-01")).toBe("qui., 01/01");
  });
});
```

`web/src/utils/weatherIconUrl.test.js`:

```js
import { describe, expect, it } from "vitest";
import { weatherIconUrl } from "./weatherIconUrl";

describe("weatherIconUrl", () => {
  it("monta a URL do ícone da OpenWeather em 2x", () => {
    expect(weatherIconUrl("01d")).toBe("https://openweathermap.org/img/wn/01d@2x.png");
  });
});
```

- [ ] **Passo 2: Testes de hooks**

`web/src/hooks/useForecast.test.js`:

```js
import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getForecastByCity } from "../services/weather";
import useForecast from "./useForecast";

vi.mock("../services/weather", () => ({ getForecastByCity: vi.fn() }));

describe("useForecast", () => {
  it("refaz a carga quando a cidade muda", async () => {
    getForecastByCity.mockImplementation(async (cityName) => ({ city: cityName, days: [] }));
    const { result, rerender } = renderHook(({ cityName }) => useForecast(cityName), {
      initialProps: { cityName: "Lisboa" },
    });
    await waitFor(() => expect(result.current.data?.city).toBe("Lisboa"));

    rerender({ cityName: "Recife" });

    await waitFor(() => expect(result.current.data?.city).toBe("Recife"));
  });
});
```

`web/src/hooks/useCitySearch.test.jsx`:

```jsx
import { act, renderHook, screen, waitFor } from "@testing-library/react";
import { App } from "antd";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { searchLocations } from "../services/locations";
import useCitySearch from "./useCitySearch";

vi.mock("../services/locations", () => ({ searchLocations: vi.fn() }));

const wrapper = ({ children }) => <App>{children}</App>;

describe("useCitySearch", () => {
  beforeEach(() => {
    searchLocations.mockReset();
  });

  it("ignora termo com menos de 2 caracteres", async () => {
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    await act(() => result.current.search(" a "));

    expect(searchLocations).not.toHaveBeenCalled();
    expect(result.current.query).toBe("");
  });

  it("busca o termo sem espaços e devolve os resultados", async () => {
    searchLocations.mockResolvedValue([{ name: "Tóquio", state: null, country: "JP" }]);
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    await act(() => result.current.search("  Tóquio "));

    expect(searchLocations).toHaveBeenCalledWith("Tóquio");
    expect(result.current.query).toBe("Tóquio");
    expect(result.current.results).toHaveLength(1);
    expect(result.current.isSearching).toBe(false);
  });

  it("erro limpa os resultados e mostra a mensagem", async () => {
    searchLocations.mockRejectedValue({
      response: { status: 400, data: { title: "Parâmetro 'query' inválido." }, headers: {} },
    });
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    await act(() => result.current.search("Tóquio"));

    expect(result.current.results).toEqual([]);
    expect(await screen.findByText("Parâmetro 'query' inválido.")).toBeInTheDocument();
  });

  it("descarta a resposta de uma busca anterior", async () => {
    let resolveOld;
    searchLocations
      .mockImplementationOnce(() => new Promise((resolve) => { resolveOld = resolve; }))
      .mockResolvedValueOnce([{ name: "Recife", state: "Pernambuco", country: "BR" }]);
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    act(() => { result.current.search("Lisboa"); });
    await act(() => result.current.search("Recife"));
    await act(async () => resolveOld([{ name: "Lisboa", state: null, country: "PT" }]));

    await waitFor(() => expect(result.current.query).toBe("Recife"));
    expect(result.current.results[0].name).toBe("Recife");
  });

  it("clear volta ao estado inicial", async () => {
    searchLocations.mockResolvedValue([{ name: "Tóquio", state: null, country: "JP" }]);
    const { result } = renderHook(() => useCitySearch(), { wrapper });
    await act(() => result.current.search("Tóquio"));

    act(() => result.current.clear());

    expect(result.current.query).toBe("");
    expect(result.current.results).toEqual([]);
  });
});
```

- [ ] **Passo 3: Rodar e ver falhar**

Run: `npm --prefix web test`
Esperado: FAIL — módulos `./locations`, `./formatWeekday`, `./weatherIconUrl`, `./useForecast`, `./useCitySearch` não encontrados.

- [ ] **Passo 4: Implementar serviços e utilitários**

`web/src/services/locations.js`:

```js
import client from "./client";

export const searchLocations = async (query) => {
  const response = await client.get("/locations", { params: { query } });
  return response.data;
};
```

Em `web/src/services/weather.js`, acrescentar:

```js
export const getForecastByCity = async (cityName) => {
  const response = await client.get(`/forecasts/${encodeURIComponent(cityName)}`);
  return response.data;
};
```

`web/src/utils/formatWeekday.js`:

```js
const formatter = new Intl.DateTimeFormat("pt-BR", {
  weekday: "short",
  day: "2-digit",
  month: "2-digit",
  timeZone: "UTC",
});

export const formatWeekday = (isoDate) => formatter.format(new Date(`${isoDate}T00:00:00Z`));
```

Se o ICU do Node formatar diferente de `"sex., 06/11"` (ex.: sem vírgula), ajustar só o teste ao formato real depois de conferir no navegador que é o mesmo, e registrar nas notas de execução.

`web/src/utils/weatherIconUrl.js`:

```js
export const weatherIconUrl = (icon) => `https://openweathermap.org/img/wn/${icon}@2x.png`;
```

- [ ] **Passo 5: Implementar os hooks**

`web/src/hooks/useForecast.js`:

```js
import { useCallback } from "react";
import { getForecastByCity } from "../services/weather";
import useAsync from "./useAsync";

export default function useForecast(cityName) {
  const load = useCallback(() => getForecastByCity(cityName), [cityName]);
  return useAsync(load);
}
```

`web/src/hooks/useCitySearch.js`:

```js
import { useCallback, useRef, useState } from "react";
import { searchLocations } from "../services/locations";
import useApiFeedback from "./useApiFeedback";

const MIN_QUERY_LENGTH = 2;
const INITIAL_STATE = { query: "", results: [], isSearching: false };

export default function useCitySearch() {
  const notifyError = useApiFeedback();
  const [state, setState] = useState(INITIAL_STATE);
  const latestSearch = useRef(0);

  const search = useCallback(
    async (value) => {
      const query = value.trim();

      if (query.length < MIN_QUERY_LENGTH) {
        return;
      }

      const searchId = ++latestSearch.current;
      setState({ query, results: [], isSearching: true });

      try {
        const results = await searchLocations(query);

        if (searchId === latestSearch.current) {
          setState({ query, results, isSearching: false });
        }
      } catch (err) {
        if (searchId === latestSearch.current) {
          setState({ query, results: [], isSearching: false });
          notifyError(err, "Não foi possível buscar cidades agora.");
        }
      }
    },
    [notifyError]
  );

  const clear = useCallback(() => {
    latestSearch.current += 1;
    setState(INITIAL_STATE);
  }, []);

  return { ...state, search, clear };
}
```

- [ ] **Passo 6: `WeatherDetails.jsx` usa `weatherIconUrl`** — `import { weatherIconUrl } from "../utils/weatherIconUrl";` e no `<img>` do hero:

```jsx
                src={weatherIconUrl(weather.icon)}
```

- [ ] **Passo 7: Rodar e ver passar**

Run: `npm --prefix web test`
Esperado: todos `passed` (35 + 10 = 45).

- [ ] **Passo 8: Verificação completa** e **commit**

```bash
git add web/src
git commit -m "Adiciona busca de cidades, previsão e utilitários de data e ícone no web"
```

---

### Tarefa 3: Busca de cidades na Home

**Arquivos:**
- Criar: `web/src/components/CitySearch.jsx`, `CitySearch.module.css`
- Modificar: `web/src/pages/Home.jsx`, `web/src/pages/Home.module.css`

**Interfaces:**
- Consome: `useCitySearch` (Tarefa 2); `EmptyState` (07a).
- Produz: `CitySearch` (sem props) — escolha navega para `/p/weather/:nome`.

- [ ] **Passo 1: Criar `web/src/components/CitySearch.jsx`**

```jsx
import { useState } from "react";
import { Card, Input, Skeleton, Typography } from "antd";
import { EnvironmentOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import useCitySearch from "../hooks/useCitySearch";
import EmptyState from "./EmptyState";
import styles from "./CitySearch.module.css";

const LIST_ID = "city-search-results";
const optionId = (index) => `${LIST_ID}-${index}`;

export default function CitySearch() {
  const navigate = useNavigate();
  const { query, results, isSearching, search, clear } = useCitySearch();
  const [activeIndex, setActiveIndex] = useState(0);

  const open = (location) => navigate(`/p/weather/${encodeURIComponent(location.name)}`);

  const handleSearch = (value) => {
    if (results.length > 0 && value.trim() === query) {
      open(results[activeIndex]);
      return;
    }

    setActiveIndex(0);
    search(value);
  };

  const handleKeyDown = (e) => {
    if (e.key === "Escape") {
      clear();
      return;
    }

    if (results.length === 0) {
      return;
    }

    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((index) => (index + 1) % results.length);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((index) => (index - 1 + results.length) % results.length);
    }
  };

  const handleChange = (e) => {
    if (e.target.value === "") {
      clear();
    }
  };

  return (
    <div className={styles.root}>
      <Input.Search
        placeholder="Buscar cidade..."
        enterButton="Buscar"
        size="large"
        allowClear
        loading={isSearching}
        role="combobox"
        aria-expanded={results.length > 0}
        aria-controls={LIST_ID}
        aria-activedescendant={results.length > 0 ? optionId(activeIndex) : undefined}
        onSearch={handleSearch}
        onKeyDown={handleKeyDown}
        onChange={handleChange}
      />

      {isSearching && (
        <Card className={styles.panel}>
          <Skeleton active title={false} paragraph={{ rows: 3 }} />
        </Card>
      )}

      {!isSearching && query && results.length === 0 && (
        <Card className={styles.panel}>
          <EmptyState description={`Nenhuma cidade encontrada para "${query}".`} />
        </Card>
      )}

      {!isSearching && results.length > 0 && (
        <Card className={styles.panel} size="small">
          <ul id={LIST_ID} role="listbox" aria-label="Cidades encontradas" className={styles.list}>
            {results.map((location, index) => (
              <li
                key={optionId(index)}
                id={optionId(index)}
                role="option"
                aria-selected={index === activeIndex}
                className={index === activeIndex ? `${styles.option} ${styles.active}` : styles.option}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => open(location)}
              >
                <EnvironmentOutlined className={styles.icon} />
                <span className={styles.name}>{location.name}</span>
                <Typography.Text type="secondary">
                  {[location.state, location.country].filter(Boolean).join(", ")}
                </Typography.Text>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}
```

- [ ] **Passo 2: Criar `web/src/components/CitySearch.module.css`**

```css
.root {
  width: 100%;
  max-width: 40rem;
  display: flex;
  flex-direction: column;
  gap: var(--ant-margin-xs);
  text-align: left;
}

.panel {
  box-shadow: var(--ant-box-shadow-secondary);
}

.list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.option {
  display: flex;
  align-items: center;
  gap: var(--ant-margin-xs);
  padding: var(--ant-padding-xs) var(--ant-padding-sm);
  border-radius: var(--ant-border-radius);
  cursor: pointer;
}

.active {
  background: var(--ant-color-primary-bg);
}

.icon {
  color: var(--ant-color-primary);
}

.name {
  font-weight: 500;
}
```

- [ ] **Passo 3: `Home.jsx` usa o `CitySearch`**

```jsx
import { Typography } from "antd";
import CityGrid from "../components/CityGrid";
import CitySearch from "../components/CitySearch";
import useCities from "../hooks/useCities";
import styles from "./Home.module.css";

export default function Home() {
  const { data, isLoading, error, reload } = useCities();

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <Typography.Title level={2}>Como está o tempo hoje?</Typography.Title>
        <Typography.Paragraph type="secondary">
          Pesquise uma cidade e escolha na lista para ver o clima agora.
        </Typography.Paragraph>
        <CitySearch />
      </section>

      <section>
        <Typography.Title level={4}>Cidades pesquisadas</Typography.Title>
        <CityGrid
          cities={data}
          isLoading={isLoading}
          error={error}
          reload={reload}
          emptyDescription="Pesquise uma cidade para começar."
        />
      </section>
    </div>
  );
}
```

E remover a regra `.search` de `Home.module.css` (a largura agora é do `CitySearch`).

- [ ] **Passo 4: Verificação completa.**

- [ ] **Passo 5: Conferência manual**
  - “Tóquio” + Enter → lista com “Tóquio · JP”; Enter de novo → detalhe com “Tóquio, JP”.
  - “Springfield” → até 5 itens com estado; setas mudam o destaque; clique abre.
  - “Xyzabc” → estado vazio com o termo; Esc limpa; “a” → nada acontece.
  - Gateway parado → `notification`/`message` de erro (sem travar o campo).
  - 375 px e tema escuro legíveis.

- [ ] **Passo 6: Commit**

```bash
git add web/src
git commit -m "Adiciona a busca de cidades com lista de resultados na Home"
```

---

### Tarefa 4: Temperatura nos cards

**Arquivos:**
- Criar: `web/src/components/CityTemperature.jsx`, `CityTemperature.module.css`
- Modificar: `web/src/components/CityCard.jsx`, `CityCard.module.css`

**Interfaces:**
- Consome: `useWeather` (07a), `weatherIconUrl` (Tarefa 2).
- Produz: `CityTemperature({ cityName })`.

- [ ] **Passo 1: Criar `web/src/components/CityTemperature.jsx`**

```jsx
import { Skeleton, Tooltip, Typography } from "antd";
import useWeather from "../hooks/useWeather";
import { weatherIconUrl } from "../utils/weatherIconUrl";
import styles from "./CityTemperature.module.css";

export default function CityTemperature({ cityName }) {
  const { data, isLoading, error } = useWeather(cityName);

  if (isLoading) {
    return <Skeleton.Button active size="small" />;
  }

  if (error) {
    return (
      <Tooltip title="Clima indisponível">
        <Typography.Text type="secondary" aria-label="Clima indisponível">
          —
        </Typography.Text>
      </Tooltip>
    );
  }

  return (
    <div className={styles.root}>
      <img src={weatherIconUrl(data.icon)} alt={data.description} className={styles.icon} />
      <Typography.Text strong className={styles.value}>
        {Math.round(data.temperature)}°C
      </Typography.Text>
    </div>
  );
}
```

- [ ] **Passo 2: Criar `web/src/components/CityTemperature.module.css`**

```css
.root {
  display: flex;
  align-items: center;
}

.icon {
  width: 2.5rem;
  height: 2.5rem;
}

.root .value {
  font-size: var(--ant-font-size-heading-4);
}
```

- [ ] **Passo 3: `CityCard.jsx`** — `import CityTemperature from "./CityTemperature";` e trocar o botão de favorito solto por um rodapé:

```jsx
      <div className={styles.footer}>
        <Button
          size="small"
          type={city.isFavorite ? "default" : "primary"}
          icon={city.isFavorite ? <StarFilled /> : <StarOutlined />}
          loading={isSaving}
          onClick={handleFavorite}
        >
          {city.isFavorite ? "Favorita" : "Favoritar"}
        </Button>
        <CityTemperature cityName={city.name} />
      </div>
```

- [ ] **Passo 4: `CityCard.module.css`** — trocar a regra `.favorite` por:

```css
.footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--ant-margin-xs);
  margin-top: var(--ant-margin);
}
```

- [ ] **Passo 5: Verificação completa.**

- [ ] **Passo 6: Conferência manual**
  - Home com 2+ cidades: cada card mostra skeleton e depois ícone + temperatura; favoritar não refaz a carga da temperatura (Network).
  - Provider parado e cache vazio: cards mostram “—” com tooltip, sem enxurrada de notificações.
  - **Histórico antigo em inglês** (“Tokyo”): o card resolve como “Tóquio” e o Gateway grava uma linha nova “Tóquio” — esperado (design §1, só dados de desenvolvimento); registrar nas notas.

- [ ] **Passo 7: Commit**

```bash
git add web/src
git commit -m "Mostra a temperatura atual nos cards de cidade"
```

---

### Tarefa 5: Previsão de 5 dias com gráfico

**Arquivos:**
- Modificar: `web/package.json`, `web/package-lock.json` (via npm), `web/src/pages/WeatherDetails.jsx`
- Criar: `web/src/components/ForecastSection.jsx`, `ForecastSection.module.css`, `ForecastChart.jsx`, `ForecastChart.module.css`, `ForecastDays.jsx`, `ForecastDays.module.css`

**Interfaces:**
- Consome: `useForecast`, `formatWeekday`, `weatherIconUrl` (Tarefa 2); `ErrorState` (07a).
- Produz: `ForecastSection({ cityName })`; `ForecastChart({ days })`; `ForecastDays({ days })`.

- [ ] **Passo 1: Instalar o Recharts**

```bash
npm --prefix web install recharts@^3.10.1
```

Esperado: `recharts` em `dependencies`.

- [ ] **Passo 2: Criar `web/src/components/ForecastChart.jsx`**

```jsx
import { theme } from "antd";
import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatWeekday } from "../utils/formatWeekday";
import styles from "./ForecastChart.module.css";

export default function ForecastChart({ days }) {
  const { token } = theme.useToken();

  const data = days.map((day) => ({
    day: formatWeekday(day.date),
    min: Math.round(day.tempMin),
    max: Math.round(day.tempMax),
    rain: day.precipitationProbability,
  }));

  const tick = { fill: token.colorTextSecondary, fontSize: token.fontSizeSM };

  return (
    <div
      className={styles.root}
      role="img"
      aria-label="Gráfico de temperatura mínima, máxima e chance de chuva dos próximos dias. Os valores estão nos cards abaixo."
    >
      <ResponsiveContainer width="100%" height="100%">
        <ComposedChart data={data} margin={{ top: 8, right: 0, bottom: 0, left: -16 }}>
          <CartesianGrid stroke={token.colorBorderSecondary} vertical={false} />
          <XAxis dataKey="day" tick={tick} stroke={token.colorBorder} />
          <YAxis yAxisId="temp" unit="°" tick={tick} stroke={token.colorBorder} />
          <YAxis
            yAxisId="rain"
            orientation="right"
            unit="%"
            domain={[0, 100]}
            tick={tick}
            stroke={token.colorBorder}
          />
          <Tooltip
            contentStyle={{
              background: token.colorBgElevated,
              border: `1px solid ${token.colorBorderSecondary}`,
              borderRadius: token.borderRadius,
            }}
            labelStyle={{ color: token.colorText }}
          />
          <Legend />
          <Bar
            yAxisId="rain"
            dataKey="rain"
            name="Chance de chuva"
            unit="%"
            fill={token.colorPrimaryBorder}
            radius={[4, 4, 0, 0]}
          />
          <Line
            yAxisId="temp"
            type="monotone"
            dataKey="max"
            name="Máxima"
            unit="°C"
            stroke={token.colorWarning}
            strokeWidth={2}
          />
          <Line
            yAxisId="temp"
            type="monotone"
            dataKey="min"
            name="Mínima"
            unit="°C"
            stroke={token.colorPrimary}
            strokeWidth={2}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}
```

`web/src/components/ForecastChart.module.css`:

```css
.root {
  width: 100%;
  height: 16rem;
}
```

- [ ] **Passo 3: Criar `web/src/components/ForecastDays.jsx` e `.module.css`**

```jsx
import { Card, Typography } from "antd";
import { formatWeekday } from "../utils/formatWeekday";
import { weatherIconUrl } from "../utils/weatherIconUrl";
import styles from "./ForecastDays.module.css";

export default function ForecastDays({ days }) {
  return (
    <ul className={styles.grid} aria-label="Previsão por dia">
      {days.map((day) => (
        <li key={day.date}>
          <Card size="small" className={styles.day}>
            <Typography.Text strong>{formatWeekday(day.date)}</Typography.Text>
            <img src={weatherIconUrl(day.icon)} alt="" className={styles.icon} />
            <Typography.Text type="secondary" className={styles.description}>
              {day.description}
            </Typography.Text>
            <Typography.Text>
              <strong>{Math.round(day.tempMax)}°</strong> / {Math.round(day.tempMin)}°
            </Typography.Text>
            <Typography.Text type="secondary">Chuva {day.precipitationProbability}%</Typography.Text>
          </Card>
        </li>
      ))}
    </ul>
  );
}
```

```css
.grid {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
  gap: var(--ant-margin-sm);
}

.day {
  height: 100%;
  text-align: center;
}

.day :global(.ant-card-body) {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--ant-margin-xxs);
}

.icon {
  width: 3rem;
  height: 3rem;
}

.description {
  text-transform: capitalize;
}
```

- [ ] **Passo 4: Criar `web/src/components/ForecastSection.jsx` e `.module.css`**

```jsx
import { Card, Skeleton, Typography } from "antd";
import useForecast from "../hooks/useForecast";
import ErrorState from "./ErrorState";
import ForecastChart from "./ForecastChart";
import ForecastDays from "./ForecastDays";
import styles from "./ForecastSection.module.css";

function ForecastContent({ data, isLoading, error, reload }) {
  if (isLoading) {
    return (
      <Card>
        <Skeleton active paragraph={{ rows: 6 }} />
      </Card>
    );
  }

  if (error) {
    return (
      <ErrorState
        error={error}
        fallback="Não foi possível carregar a previsão agora."
        onRetry={reload}
      />
    );
  }

  return (
    <Card>
      <div className={styles.content}>
        <ForecastChart days={data.days} />
        <ForecastDays days={data.days} />
      </div>
    </Card>
  );
}

export default function ForecastSection({ cityName }) {
  const forecast = useForecast(cityName);

  return (
    <section>
      <Typography.Title level={4}>Próximos 5 dias</Typography.Title>
      <ForecastContent {...forecast} />
    </section>
  );
}
```

```css
.content {
  display: flex;
  flex-direction: column;
  gap: var(--ant-margin-lg);
}
```

- [ ] **Passo 5: `WeatherDetails.jsx`** — `import ForecastSection from "../components/ForecastSection";` e, depois do `<Row>` das métricas, ainda dentro de `<div className={styles.page}>`:

```jsx
      <ForecastSection cityName={cityName} />
```

- [ ] **Passo 6: Verificação completa.** Conferir o tamanho do bundle no build (`dist/assets/index-*.js`); registrar nas notas.

- [ ] **Passo 7: Conferência manual**
  - Detalhe de “Tóquio”: seção “Próximos 5 dias” com skeleton, depois gráfico (linhas mín/máx, barras de chuva, tooltip em português) e 5 cards.
  - Tema escuro: eixos, grade e tooltip legíveis (cores do token).
  - 375 px: gráfico ocupa a largura; cards em 2 colunas.
  - Previsão com erro (simular parando o Provider depois de o clima entrar no cache): só a seção mostra `ErrorState`; o clima atual continua.

- [ ] **Passo 8: Commit**

```bash
git add web/package.json web/package-lock.json web/src
git commit -m "Adiciona a previsão de 5 dias com gráfico no detalhe do clima"
```

---

### Tarefa 6: Documentação e fechamento da Fase 07

**Arquivos:**
- Modificar: `docs/architecture/backend.md`, `docs/architecture/frontend.md`, `docs/architecture/review-checklist.md`, `docs/project-overview.md`, este plano (checkboxes + notas)

- [ ] **Passo 1: `backend.md`**
  - **Comunicação HTTP**, item de timeouts: trocar “OpenWeather 15 s” por “OpenWeather 2 × 9 s (geocoding + clima)” e “Provider → OpenWeather 5 s/15 s” por “Provider → OpenWeather 4 s/9 s”.
  - Novo item depois do do `IOpenWeatherClient`: “Clima e previsão resolvem a cidade antes (`ICityResolver`/`CityResolver`): primeiro resultado do `geo/1.0/direct`, nome em português (`local_names.pt`, senão o original) e coordenadas; a consulta à OpenWeather é por `lat`/`lon` (formatados com `CultureInfo.InvariantCulture`). `City`/`Country` da resposta vêm do resolvido; a imagem da Pexels usa o nome original.”
  - **Cache**, TTLs: acrescentar “`cidades` 24 h (só cidade encontrada; vazio não é cacheado)”.
  - Árvore do Application: “AppServices (clima, previsão, busca, imagem), `CityResolver` e cache (Services)”.

- [ ] **Passo 2: `frontend.md`**
  - **Chamadas à API**: trocar o item da previsão/busca por: “Busca de cidades (`GET /locations?query=`, mínimo 2 caracteres) é ao confirmar: `CitySearch` + `useCitySearch` mostram até 5 resultados para escolher (setas/Enter/Esc); a escolha navega só pelo nome (homônimos caem na primeira cidade). Previsão (`GET /forecasts/{cidade}`) com `useForecast` em seção própria: falha da previsão não derruba o detalhe.”
  - **Tema e estilo**: “Gráficos com Recharts; cores só de `theme.useToken()` (nada de hex nas props). Todo gráfico tem `role="img"` com `aria-label` e uma leitura em texto ao lado (ex.: cards da previsão).”
  - **Estrutura**: `utils/` passa a citar `formatTime, formatWeekday, weatherIconUrl`; `hooks/` ganha `useForecast, useCitySearch`; `services/` ganha `locations.js`.

- [ ] **Passo 3: `review-checklist.md`**
  - BE-28, coluna “Como verificar”: acrescentar “ou URL com `lat`/`lon` montada fora dele”.
  - FE-06, coluna “Como verificar”: acrescentar “(inclui props de cor do Recharts: só `token.*`)”.

- [ ] **Passo 4: `project-overview.md`**
  - Título: `#### Fase 07 — Frontend moderno ✅ Concluída (AAAA-MM-DD)`; linha da 07b: `- **07b — Busca e previsão** ✅ (AAAA-MM-DD): busca de cidades com lista, previsão de 5 dias com gráfico (Recharts), temperatura nos cards e nome da cidade em português (Provider resolve no geocoding e consulta por coordenadas). [Design](features/07-frontend-moderno/busca-previsao-design.md) · [Plano](features/07-frontend-moderno/busca-previsao-plan.md)`.
  - **Estado atual**: data e “após a Fase 07”; em Funciona, trocar “previsão de 5 dias e busca de cidades na API (ainda sem tela no front)” por “busca de cidades, previsão de 5 dias com gráfico e temperatura nos cards, nomes de cidade em português”; Stack ganha `Recharts`.
  - **Pendências**: remover as duas `[Fase 07b]` (busca e nome); acrescentar `- [Backlog] Homônimos ("Springfield"): a busca mostra estado/país, mas clima e previsão usam a primeira cidade do geocoding — identificar por coordenadas exige mudar contrato, URL e entidade City`.

- [ ] **Passo 5: Verificação final** (skill `superpowers:verification-before-completion`) — os comandos da seção Verificação do `CLAUDE.md`, incluindo `docker compose build` (com `.env` local; sem `.env`, criar um temporário só com valores de build e apagar depois).

- [ ] **Passo 6: Notas de execução** — `## Notas de execução (AAAA-MM-DD)` no fim deste plano; marcar os checkboxes.

- [ ] **Passo 7: `/revisar-padrao`** na branch inteira; com 🔴, parar. Depois `/requesting-code-review`.

- [ ] **Passo 8: Commit final**

```bash
git add docs
git commit -m "Documenta busca e previsão e fecha a Fase 07"
```

Depois, `/finishing-a-development-branch` → PR `dev/busca-previsao` → `master`. PR com CI ❌ não é mergeado.
