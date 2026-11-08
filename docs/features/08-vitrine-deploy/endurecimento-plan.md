# Fase 08a — Endurecimento: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans para executar este plano tarefa a tarefa. Passos com checkbox (`- [ ]`).

**Goal:** deixar o Climato pronto para exposição pública: histórico sem efeito colateral no GET, cards sem Pexels, rate limiting, usuário de demonstração, 400 em português, `traceparent` do cliente ignorado, health checks e 503 rápido com o Provider fora.

**Architecture:** mudanças concentradas no Gateway (controllers, `Api/` para rate limit/erros/observabilidade, `Application` para regras), um parâmetro novo no Provider (`image`) e ajustes pontuais no web (`services/`, `WeatherDetails`, `CityTemperature`, `SignIn`).

**Tech Stack:** .NET 10 (ASP.NET Core rate limiting nativo, `Microsoft.Extensions.Http.Resilience`, OpenTelemetry 1.20), xUnit v3 + Shouldly + NSubstitute; React 19 + Vitest.

**Design:** [`endurecimento-design.md`](endurecimento-design.md)

## Global Constraints

- Ler [`backend.md`](../../architecture/backend.md) e [`frontend.md`](../../architecture/frontend.md) antes de cada tarefa da camada.
- TDD: toda regra nova nasce com teste vermelho → verde.
- Um commit por tarefa, em português, no imperativo, com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; `/revisar-padrao` antes de cada commit.
- Nenhum segredo no repositório; `Demo:*` e `RateLimiting:*` sem segredo podem ir no `appsettings.json`.
- Não subir o serviço `web` do compose na porta 8080 desta máquina (ocupada por outro projeto); conferência manual do web via `npm --prefix web run dev`.
- Mensagens ao usuário em português.

## Investigação prévia (item 8 do design)

Medido com um `HttpClient` isolado nesta máquina (Windows):

| Destino | Sem `ConnectTimeout` | Com `ConnectTimeout = 2 s` |
|---|---|---|
| Host inalcançável (`10.255.255.1`) | 21 s, `HttpRequestException` | 2 s, `TaskCanceledException` (inner `TimeoutException`) |
| Porta recusada em `localhost` | 4 s, `HttpRequestException` | 2 s, `TaskCanceledException` (inner `TimeoutException`) |
| Nome inexistente | 30 ms, `HttpRequestException` | igual |

**Causa:** a conexão TCP não tem limite próprio; um host que não responde segura a tentativa até o SYN expirar (~21 s), e a retentativa estoura os 30 s totais. **Correção:** `ConnectTimeout` de 2 s, espera de retentativa de 500 ms e tratar o `TaskCanceledException` de conexão como falha de transporte (retentativa e 503). Pior caso: 2 + 0,5 + 2 ≈ 4,5 s.

---

### Task 1: Histórico sem efeito colateral (`GET /weathers` só lê, `POST /cities`)

**Files:**
- Modify: `src/Gateway/Climato.Gateway.Application/Contracts/IWeatherAppService.cs`
- Modify: `src/Gateway/Climato.Gateway.Application/Services/WeatherAppService.cs`
- Modify: `src/Gateway/Climato.Gateway.Api/Controllers/WeatherController.cs`
- Create: `src/Gateway/Climato.Gateway.Application/Model/Cities/CityPostDto.cs`
- Create: `src/Gateway/Climato.Gateway.Application/Validators/CityPostDtoValidator.cs`
- Modify: `src/Gateway/Climato.Gateway.Application/Contracts/ICityAppService.cs`
- Modify: `src/Gateway/Climato.Gateway.Application/Services/CityAppService.cs`
- Modify: `src/Gateway/Climato.Gateway.Api/Controllers/CityController.cs`
- Test: `tests/Climato.Gateway.UnitTests/Application/Services/WeatherAppServiceTests.cs`, `CityAppServiceTests.cs`
- Create test: `tests/Climato.Gateway.UnitTests/Application/Validators/CityPostDtoValidatorTests.cs`
- Modify: `web/src/services/cities.js`; Create: `web/src/services/cities.test.js`
- Modify: `web/src/pages/WeatherDetails.jsx`

**Interfaces:**
- Produces: `IWeatherAppService.GetByCityAsync(string cityName, bool includeImage, CancellationToken)` (o `includeImage` entra na Task 2; nesta task a assinatura fica `GetByCityAsync(string cityName, CancellationToken)`); `ICityAppService.AddAsync(CityPostDto dto, Guid userId, CancellationToken) → Task<(CityDto City, bool Created)>`; web `addCity({ name, country })`.

- [x] **Step 1: Testes vermelhos do Gateway**

Em `WeatherAppServiceTests`, substituir os três testes de histórico por:

```csharp
[Fact]
public async Task GetByCityAsync_ProviderNaoEncontra_DevolveNull()
{
    providerClient.GetWeatherAsync("Atlantida", Arg.Any<CancellationToken>()).Returns((WeatherDto?)null);

    var weather = await appService.GetByCityAsync("Atlantida", cancellationToken);

    weather.ShouldBeNull();
}

[Fact]
public async Task GetByCityAsync_CidadeEncontrada_DevolveOClimaSemGravarNoHistorico()
{
    var expected = TestData.Weather("Lima");
    providerClient.GetWeatherAsync("lima", Arg.Any<CancellationToken>()).Returns(expected);

    var weather = await appService.GetByCityAsync("lima", cancellationToken);

    weather.ShouldBeSameAs(expected);
}
```

O construtor passa a ser `new WeatherAppService(providerClient)` e o campo `repository` sai do teste.

Em `CityAppServiceTests`, acrescentar:

```csharp
[Fact]
public async Task AddAsync_CidadeNova_CriaNoHistoricoDoUsuario()
{
    repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns((City?)null);

    var (city, created) = await appService.AddAsync(new CityPostDto { Name = " Lima ", Country = "PE" }, userId, cancellationToken);

    created.ShouldBeTrue();
    city.Name.ShouldBe("Lima");
    city.Country.ShouldBe("PE");
    await repository.Received(1).CreateAsync(
        Arg.Is<City>(c => c.Name == "Lima" && c.Country == "PE" && c.UserId == userId && !c.IsFavorite),
        Arg.Any<CancellationToken>());
}

[Fact]
public async Task AddAsync_CidadeJaNoHistorico_DevolveAExistenteSemCriar()
{
    var existing = new City("Lima", "PE", userId);
    existing.ToggleFavorite();
    repository.GetByNameAsync("Lima", userId, Arg.Any<CancellationToken>()).Returns(existing);

    var (city, created) = await appService.AddAsync(new CityPostDto { Name = "Lima", Country = "PE" }, userId, cancellationToken);

    created.ShouldBeFalse();
    city.Id.ShouldBe(existing.Id);
    city.IsFavorite.ShouldBeTrue();
    await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken);
}
```

Criar `CityPostDtoValidatorTests` no mesmo formato de `LoginPostDtoValidatorTests`:

```csharp
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Application.Validators;

namespace Climato.Gateway.UnitTests.Application.Validators;

public class CityPostDtoValidatorTests
{
    private readonly CityPostDtoValidator validator = new();

    [Fact]
    public void Validate_DadosValidos_NaoTemErros()
    {
        validator.Validate(new CityPostDto { Name = "Lima", Country = "PE" }).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "PE", "Name", "Informe o nome da cidade.")]
    [InlineData("Lima", "", "Country", "Informe o país.")]
    public void Validate_CampoVazio_DevolveMensagem(string name, string country, string property, string message)
    {
        var result = validator.Validate(new CityPostDto { Name = name, Country = country });

        result.Errors.ShouldContain(error => error.PropertyName == property && error.ErrorMessage == message);
    }

    [Fact]
    public void Validate_NomeMaiorQue255_DevolveMensagem()
    {
        var result = validator.Validate(new CityPostDto { Name = new string('a', 256), Country = "PE" });

        result.Errors.ShouldContain(error => error.ErrorMessage == "O nome da cidade deve ter no máximo 255 caracteres.");
    }
}
```

- [x] **Step 2: Rodar e ver falhar**

Run: `dotnet test --solution Climato.slnx`
Expected: falha de compilação (`CityPostDto`, `AddAsync`, construtor de `WeatherAppService`).

- [x] **Step 3: Implementação do Gateway**

`CityPostDto.cs`:

```csharp
namespace Climato.Gateway.Application.Model.Cities;

public class CityPostDto
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
```

`CityPostDtoValidator.cs`:

```csharp
using Climato.Gateway.Application.Model.Cities;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class CityPostDtoValidator : AbstractValidator<CityPostDto>
{
    public CityPostDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o nome da cidade.")
            .MaximumLength(255).WithMessage("O nome da cidade deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Country)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o país.")
            .MaximumLength(255).WithMessage("O país deve ter no máximo 255 caracteres.");
    }
}
```

`ICityAppService`: `Task<(CityDto City, bool Created)> AddAsync(CityPostDto dto, Guid userId, CancellationToken cancellationToken);`

`CityAppService.AddAsync`:

```csharp
public async Task<(CityDto City, bool Created)> AddAsync(CityPostDto dto, Guid userId, CancellationToken cancellationToken)
{
    var name = dto.Name.Trim();
    var existing = await cityRepository.GetByNameAsync(name, userId, cancellationToken);

    if (existing != null)
    {
        return (mapper.Map<CityDto>(existing), false);
    }

    var city = new City(name, dto.Country.Trim(), userId);
    await cityRepository.CreateAsync(city, cancellationToken);

    return (mapper.Map<CityDto>(city), true);
}
```

`WeatherAppService`: remover `ICityRepository`, `userId` e a gravação:

```csharp
public class WeatherAppService : IWeatherAppService
{
    private readonly IWeatherProviderClient weatherProviderClient;

    public WeatherAppService(IWeatherProviderClient providerClient)
    {
        weatherProviderClient = providerClient;
    }

    public Task<WeatherDto?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        return weatherProviderClient.GetWeatherAsync(cityName, cancellationToken);
    }

    public Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        return weatherProviderClient.GetForecastAsync(cityName, cancellationToken);
    }
}
```

`WeatherController`: `<summary>Retorna os dados climáticos da cidade informada.</summary>`, `404` → "Cidade não encontrada.", chamada `appService.GetByCityAsync(cityName, cancellationToken)`.

`CityController`, nova action:

```csharp
/// <summary>Registra a cidade no histórico do usuário autenticado (idempotente).</summary>
/// <response code="200">Cidade já estava no histórico.</response>
/// <response code="201">Cidade adicionada ao histórico.</response>
/// <response code="400">Dados inválidos.</response>
[HttpPost]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<CityDto>> Add(CityPostDto dto, CancellationToken cancellationToken)
{
    var (city, created) = await appService.AddAsync(dto, User.GetUserId(), cancellationToken);

    return created ? StatusCode(StatusCodes.Status201Created, city) : Ok(city);
}
```

- [x] **Step 4: Rodar e ver passar**

Run: `dotnet test --solution Climato.slnx`
Expected: todos verdes.

- [x] **Step 5: Web — teste vermelho de `addCity`**

`web/src/services/cities.test.js`:

```js
import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { addCity } from "./cities";

vi.mock("./client", () => ({ default: { post: vi.fn() } }));

describe("addCity", () => {
  it("envia nome e país para /cities", async () => {
    client.post.mockResolvedValue({ data: { id: "1", name: "Lima", country: "PE" } });

    const result = await addCity({ name: "Lima", country: "PE" });

    expect(client.post).toHaveBeenCalledWith("/cities", { name: "Lima", country: "PE" });
    expect(result).toEqual({ id: "1", name: "Lima", country: "PE" });
  });
});
```

Run: `npm --prefix web test` → FAIL (`addCity` não exportado).

- [x] **Step 6: Web — implementação**

`cities.js`:

```js
export const addCity = async ({ name, country }) => {
  const response = await client.post("/cities", { name, country });
  return response.data;
};
```

`WeatherDetails.jsx`: importar `useEffect`, `useApiFeedback` e `addCity`; depois do `useWeather`:

```jsx
const notifyError = useApiFeedback();

useEffect(() => {
  if (!weather) {
    return;
  }

  addCity({ name: weather.city, country: weather.country }).catch((err) =>
    notifyError(err, "Não foi possível salvar a cidade no histórico.")
  );
}, [weather, notifyError]);
```

Run: `npm --prefix web test` e `npm --prefix web run lint` → verdes.

- [x] **Step 7: Conferência manual**

Gateway + Provider locais (runbook) e `npm --prefix web run dev`: abrir a Home com histórico → nenhuma linha nova em `city`; buscar e abrir uma cidade nova → aparece na Home ao voltar.

- [x] **Step 8: `/revisar-padrao` e commit**

```bash
git add src/Gateway tests/Climato.Gateway.UnitTests web/src/services/cities.js web/src/services/cities.test.js web/src/pages/WeatherDetails.jsx
git commit -m "Separa a consulta de clima do registro no histórico"
```

---

### Task 2: Cards sem Pexels (`image=false`)

**Files:**
- Modify: `src/Provider/Climato.Provider.Application/Contracts/IWeatherAppService.cs`, `Services/WeatherAppService.cs`
- Modify: `src/Provider/Climato.Provider.Api/Controllers/WeatherController.cs`
- Modify: `src/Gateway/Climato.Gateway.Application/Contracts/IWeatherProviderClient.cs`, `Contracts/IWeatherAppService.cs`, `Services/WeatherAppService.cs`
- Modify: `src/Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs`
- Modify: `src/Gateway/Climato.Gateway.Api/Controllers/WeatherController.cs`
- Test: `tests/Climato.Provider.UnitTests/Application/Services/WeatherAppServiceTests.cs`, `tests/Climato.Gateway.UnitTests/Application/Services/WeatherAppServiceTests.cs`
- Create test: `tests/Climato.Gateway.UnitTests/Infrastructure/Http/WeatherProviderClientTests.cs`
- Modify: `web/src/services/weather.js`, `web/src/hooks/useWeather.js`, `web/src/hooks/useWeather.test.js`, `web/src/components/CityTemperature.jsx`; Create: `web/src/services/weather.test.js`

**Interfaces:**
- Produces: Provider `IWeatherAppService.GetByCityAsync(string cityName, bool includeImage, CancellationToken)`; Gateway `IWeatherProviderClient.GetWeatherAsync(string cityName, bool includeImage, CancellationToken)` e `IWeatherAppService.GetByCityAsync(string cityName, bool includeImage, CancellationToken)`; web `getWeatherByCity(cityName, { image = true } = {})`, `useWeather(cityName, { image = true } = {})`.

- [x] **Step 1: Testes vermelhos do Provider**

Atualizar as chamadas existentes para `GetByCityAsync(x, includeImage: true, cancellationToken)` e acrescentar:

```csharp
[Fact]
public async Task GetByCityAsync_SemImagem_NaoChamaAPexelsEUsaCacheProprio()
{
    Resolves("Lima", lima);
    openWeatherClient.GetCurrentAsync(-12.04, -77.03, Arg.Any<CancellationToken>())
        .Returns(Current("Miraflores", TimeSpan.FromHours(-5), Utc(2026, 10, 26, 10), Utc(2026, 10, 26, 23)));

    var weather = await appService.GetByCityAsync("Lima", includeImage: false, cancellationToken);

    weather.ShouldNotBeNull();
    weather.ImageUrl.ShouldBeNull();
    await cityImageAppService.DidNotReceiveWithAnyArgs().GetImageUrlAsync(default!, cancellationToken);
    await cache.Received(1).SetAsync("clima-sem-imagem", "Lima", Arg.Any<Weather>(), TimeSpan.FromMinutes(10), Arg.Any<CancellationToken>());
}

[Fact]
public async Task GetByCityAsync_SemImagemNoCache_LeDoCacheSemImagem()
{
    var cached = new Weather { City = "Lima", Country = "PE" };
    cache.GetAsync<Weather>("clima-sem-imagem", "Lima", Arg.Any<CancellationToken>()).Returns(cached);

    var weather = await appService.GetByCityAsync("Lima", includeImage: false, cancellationToken);

    weather.ShouldBeSameAs(cached);
    await cityResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, cancellationToken);
}
```

(Ajustar o inicializador de `Weather` aos membros obrigatórios do tipo, como no teste `GetByCityAsync_ClimaNoCache_...` existente.)

- [x] **Step 2: Rodar e ver falhar** — `dotnet test --solution Climato.slnx` → erro de compilação.

- [x] **Step 3: Implementação do Provider**

```csharp
private const string CachePrefix = "clima";
private const string CachePrefixWithoutImage = "clima-sem-imagem";

public async Task<Weather?> GetByCityAsync(string cityName, bool includeImage, CancellationToken cancellationToken)
{
    var cachePrefix = includeImage ? CachePrefix : CachePrefixWithoutImage;
    var cached = await cache.GetAsync<Weather>(cachePrefix, cityName, cancellationToken);
    // ... igual até o current

    var imageUrl = includeImage
        ? await cityImageAppService.GetImageUrlAsync(city.OriginalName, cancellationToken)
        : null;
    var weather = ToWeather(city, current, imageUrl);

    await cache.SetAsync(cachePrefix, cityName, weather, cacheDuration, cancellationToken);
    return weather;
}
```

`WeatherController` (Provider): `Get([FromQuery] string cityName, [FromQuery] bool image = true, ...)` → `appService.GetByCityAsync(cityName, image, cancellationToken)`; documentar o parâmetro (`<param name="image">false omite a foto da Pexels (cards)</param>`).

- [x] **Step 4: Testes vermelhos do Gateway**

Em `WeatherAppServiceTests` (Gateway), trocar as chamadas para `GetByCityAsync("lima", includeImage: true, ...)` e os stubs para `GetWeatherAsync("lima", true, ...)`; acrescentar:

```csharp
[Fact]
public async Task GetByCityAsync_SemImagem_RepassaAoProvider()
{
    providerClient.GetWeatherAsync("Lima", false, Arg.Any<CancellationToken>()).Returns(TestData.Weather("Lima"));

    var weather = await appService.GetByCityAsync("Lima", includeImage: false, cancellationToken);

    weather.ShouldNotBeNull();
}
```

`tests/Climato.Gateway.UnitTests/Infrastructure/Http/WeatherProviderClientTests.cs`:

```csharp
using System.Net;
using System.Text;
using Climato.Gateway.Infrastructure.Http;

namespace Climato.Gateway.UnitTests.Infrastructure.Http;

public class WeatherProviderClientTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(true, "/weathers?cityName=S%C3%A3o%20Paulo")]
    [InlineData(false, "/weathers?cityName=S%C3%A3o%20Paulo&image=false")]
    public async Task GetWeatherAsync_MontaAUrlConformeAImagem(bool includeImage, string expectedPathAndQuery)
    {
        var handler = new CapturingHandler();
        var client = new WeatherProviderClient(new HttpClient(handler) { BaseAddress = new Uri("http://provider/") });

        await client.GetWeatherAsync("São Paulo", includeImage, cancellationToken);

        handler.Request!.RequestUri!.PathAndQuery.ShouldBe(expectedPathAndQuery);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"city\":\"São Paulo\",\"country\":\"BR\"}", Encoding.UTF8, "application/json")
            });
        }
    }
}
```

- [x] **Step 5: Rodar e ver falhar** — erro de compilação.

- [x] **Step 6: Implementação do Gateway**

`IWeatherProviderClient.GetWeatherAsync(string cityName, bool includeImage, CancellationToken)`; em `WeatherProviderClient`:

```csharp
public Task<WeatherDto?> GetWeatherAsync(string cityName, bool includeImage, CancellationToken cancellationToken)
{
    var url = $"weathers?cityName={Uri.EscapeDataString(cityName)}";
    return GetAsync<WeatherDto>(includeImage ? url : $"{url}&image=false", cancellationToken);
}
```

`IWeatherAppService`/`WeatherAppService.GetByCityAsync(string cityName, bool includeImage, CancellationToken)` repassa. `WeatherController.GetWeather(string cityName, [FromQuery] bool image = true, CancellationToken cancellationToken = default)` com `<param name="image">false omite a foto da cidade (usado pelos cards).</param>`.

Run: `dotnet test --solution Climato.slnx` → verdes.

- [x] **Step 7: Web — testes vermelhos**

`web/src/services/weather.test.js`:

```js
import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { getWeatherByCity } from "./weather";

vi.mock("./client", () => ({ default: { get: vi.fn() } }));

describe("getWeatherByCity", () => {
  it("pede o clima completo por padrão", async () => {
    client.get.mockResolvedValue({ data: { city: "Lima" } });

    await getWeatherByCity("São Paulo");

    expect(client.get).toHaveBeenLastCalledWith("/weathers/S%C3%A3o%20Paulo", { params: {} });
  });

  it("pede sem imagem quando image é false", async () => {
    client.get.mockResolvedValue({ data: { city: "Lima" } });

    await getWeatherByCity("Lima", { image: false });

    expect(client.get).toHaveBeenLastCalledWith("/weathers/Lima", { params: { image: false } });
  });
});
```

Em `useWeather.test.js`, a última asserção vira `expect(getWeatherByCity).toHaveBeenLastCalledWith("Recife", { image: true });` e acrescentar:

```js
it("repassa image false ao serviço", async () => {
  getWeatherByCity.mockResolvedValue({ city: "Lima" });
  const { result } = renderHook(() => useWeather("Lima", { image: false }));

  await waitFor(() => expect(result.current.data).toEqual({ city: "Lima" }));
  expect(getWeatherByCity).toHaveBeenLastCalledWith("Lima", { image: false });
});
```

Run: `npm --prefix web test` → FAIL.

- [x] **Step 8: Web — implementação**

```js
export const getWeatherByCity = async (cityName, { image = true } = {}) => {
  const response = await client.get(`/weathers/${encodeURIComponent(cityName)}`, {
    params: image ? {} : { image: false },
  });
  return response.data;
};
```

```js
export default function useWeather(cityName, { image = true } = {}) {
  const load = useCallback(() => getWeatherByCity(cityName, { image }), [cityName, image]);
  return useAsync(load);
}
```

`CityTemperature`: `useWeather(cityName, { image: false })`.

Run: `npm --prefix web test`, `npm --prefix web run lint` → verdes.

- [x] **Step 9: Conferência manual** — Home com cards: no log do Provider só aparecem chamadas à OpenWeather; abrir o detalhe → foto aparece.

- [x] **Step 10: `/revisar-padrao` e commit**

```bash
git add src tests web/src
git commit -m "Tira a foto da Pexels do clima dos cards"
```

---

### Task 3: Rate limiting no Gateway

**Files:**
- Create: `src/Gateway/Climato.Gateway.Api/RateLimiting/RateLimitingOptions.cs`
- Create: `src/Gateway/Climato.Gateway.Api/RateLimiting/RateLimitPolicies.cs`
- Create: `src/Gateway/Climato.Gateway.Api/RateLimiting/RateLimitingExtensions.cs`
- Modify: `src/Gateway/Climato.Gateway.Api/Program.cs`, `appsettings.json`
- Modify: todos os controllers do Gateway (`[EnableRateLimiting]`)
- Create test: `tests/Climato.Gateway.UnitTests/Api/RateLimiting/RateLimitPoliciesTests.cs`

**Interfaces:**
- Produces: `RateLimitPolicies.Auth = "auth"`, `RateLimitPolicies.Api = "api"`, `RateLimitPolicies.PartitionKey(HttpContext) → string`, `RateLimitPolicies.OnRejectedAsync(OnRejectedContext, CancellationToken) → ValueTask`.

- [x] **Step 1: Testes vermelhos**

```csharp
using System.Security.Claims;
using System.Text.Json;
using System.Net;
using System.Threading.RateLimiting;
using Climato.Gateway.Api.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Climato.Gateway.UnitTests.Api.RateLimiting;

public class RateLimitPoliciesTests
{
    [Fact]
    public void PartitionKey_UsuarioAutenticado_UsaOSub()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "abc")], "Bearer"))
        };

        RateLimitPolicies.PartitionKey(context).ShouldBe("user:abc");
    }

    [Fact]
    public void PartitionKey_Anonimo_UsaOIp()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        RateLimitPolicies.PartitionKey(context).ShouldBe("ip:203.0.113.7");
    }

    [Fact]
    public async Task OnRejectedAsync_Devolve429ComRetryAfterEProblemDetailsEmPortugues()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        await RateLimitPolicies.OnRejectedAsync(new OnRejectedContext { HttpContext = context, Lease = new RejectedLease(TimeSpan.FromSeconds(42)) }, TestContext.Current.CancellationToken);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.ToString().ShouldBe("42");
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Muitas requisições. Tente novamente em instantes.");
    }

    private sealed class RejectedLease(TimeSpan retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = metadataName == MetadataName.RetryAfter.Name ? retryAfter : null;
            return metadata != null;
        }
    }
}
```

- [x] **Step 2: Rodar e ver falhar** — erro de compilação.

- [x] **Step 3: Implementação**

`RateLimitingOptions.cs`:

```csharp
namespace Climato.Gateway.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; init; } = 10;
    public int ApiPermitLimit { get; init; } = 60;
    public int WindowSeconds { get; init; } = 60;
}
```

`RateLimitPolicies.cs`:

```csharp
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Climato.Gateway.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Api = "api";

    public static string PartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return userId != null ? $"user:{userId}" : $"ip:{context.Connection.RemoteIpAddress}";
    }

    public static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas requisições. Tente novamente em instantes."
            }
        });
    }
}
```

`RateLimitingExtensions.cs`:

```csharp
using System.Threading.RateLimiting;

namespace Climato.Gateway.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        var window = TimeSpan.FromSeconds(options.WindowSeconds);

        services.AddRateLimiter(limiter =>
        {
            limiter.OnRejected = RateLimitPolicies.OnRejectedAsync;
            limiter.AddPolicy(RateLimitPolicies.Auth, context => FixedWindow($"ip:{context.Connection.RemoteIpAddress}", options.AuthPermitLimit, window));
            limiter.AddPolicy(RateLimitPolicies.Api, context => FixedWindow(RateLimitPolicies.PartitionKey(context), options.ApiPermitLimit, window));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(string key, int permitLimit, TimeSpan window)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0
        });
    }
}
```

`DependencyInjection.AddGatewayApi`: `services.AddGatewayRateLimiting(configuration);`.
`Program.cs`: `app.UseRateLimiter();` logo após `app.UseAuthorization();`.
`appsettings.json`: `"RateLimiting": { "AuthPermitLimit": 10, "ApiPermitLimit": 60, "WindowSeconds": 60 }`.
Controllers: `[EnableRateLimiting(RateLimitPolicies.Auth)]` no `AuthController`; `[EnableRateLimiting(RateLimitPolicies.Api)]` em `CityController`, `WeatherController`, `ForecastController`, `LocationController`, `UserController`. Health checks ficam sem política.

- [x] **Step 4: Rodar e ver passar** — `dotnet test --solution Climato.slnx`.

- [x] **Step 5: Conferência manual**

```bash
for i in $(seq 1 11); do curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5001/auth/login -H "Content-Type: application/json" -d '{"email":"x@x.dev","password":"x"}'; done
```

Expected: 10 × `401`, depois `429` com `Retry-After`.

- [x] **Step 6: `/revisar-padrao` e commit**

```bash
git add src/Gateway tests/Climato.Gateway.UnitTests
git commit -m "Adiciona rate limiting por IP no auth e por usuário na API"
```

---

### Task 4: Usuário de demonstração

**Files:**
- Create: `src/Gateway/Climato.Gateway.Application/Options/DemoOptions.cs`
- Modify: `src/Gateway/Climato.Gateway.Application/Contracts/IAuthAppService.cs`, `Services/AuthAppService.cs`
- Modify: `src/Gateway/Climato.Gateway.Infrastructure/DependencyInjection.cs` (options)
- Modify: `src/Gateway/Climato.Gateway.Api/Controllers/AuthController.cs`, `appsettings.json`
- Modify: `docker-compose.yml`, `web/Dockerfile`, `web/.env.example`
- Test: `tests/Climato.Gateway.UnitTests/Application/Services/AuthAppServiceTests.cs`
- Modify: `web/src/services/auth.js`, `web/src/pages/SignIn.jsx`; Create: `web/src/services/auth.test.js`

**Desvio do design:** o usuário demo é criado **sob demanda** no primeiro `POST /auth/demo` (não no startup). Motivo: `DELETE /users/me` permite que um visitante exclua o demo; criar sob demanda recria o usuário e as cidades iniciais sem reiniciar o serviço. Mesmo efeito para quem usa, sem seeder.

**Interfaces:**
- Produces: `IAuthAppService.LoginAsDemoAsync(CancellationToken) → Task<AuthDto?>` (null = demo desligada); `DemoOptions { bool Enabled; string Email; string Name; List<DemoCity> Cities }`, `DemoCity { string Name; string Country }`; web `loginAsDemo()`.

- [x] **Step 1: Testes vermelhos**

Em `AuthAppServiceTests`, acrescentar `private readonly ICityRepository cityRepository = Substitute.For<ICityRepository>();` e um `DemoOptions` mutável:

```csharp
private readonly DemoOptions demo = new()
{
    Enabled = true,
    Email = "demo@climato.dev",
    Name = "Visitante",
    Cities = [new DemoCity { Name = "Lisboa", Country = "PT" }, new DemoCity { Name = "Tóquio", Country = "JP" }]
};
```

Construtor: `new AuthAppService(repository, cityRepository, passwordHasher, tokenService, TestMapper.Create(), Options.Create(demo))`.

```csharp
[Fact]
public async Task LoginAsDemoAsync_DemoDesligada_DevolveNullSemTocarNoBanco()
{
    demo.Enabled = false;

    var auth = await appService.LoginAsDemoAsync(cancellationToken);

    auth.ShouldBeNull();
    repository.ReceivedCalls().ShouldBeEmpty();
}

[Fact]
public async Task LoginAsDemoAsync_UsuarioDemoExiste_DevolveTokenSemCriar()
{
    repository.GetAsync("demo@climato.dev", Arg.Any<CancellationToken>()).Returns(new User("demo@climato.dev", "Visitante"));

    var auth = await appService.LoginAsDemoAsync(cancellationToken);

    auth.ShouldNotBeNull();
    auth.Token.ShouldBe("jwt-de-teste");
    await repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken);
    await cityRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken);
}

[Fact]
public async Task LoginAsDemoAsync_UsuarioDemoAusente_CriaComSenhaAleatoriaECidadesIniciais()
{
    repository.GetAsync("demo@climato.dev", Arg.Any<CancellationToken>()).Returns((User?)null);
    passwordHasher.HashPassword(Arg.Any<User>(), Arg.Any<string>()).Returns("hash-aleatorio");

    var auth = await appService.LoginAsDemoAsync(cancellationToken);

    auth.ShouldNotBeNull();
    auth.User.Email.ShouldBe("demo@climato.dev");
    passwordHasher.Received(1).HashPassword(Arg.Any<User>(), Arg.Is<string>(password => password.Length >= 32));
    await repository.Received(1).CreateAsync(Arg.Is<User>(user => user.PasswordHash == "hash-aleatorio"), Arg.Any<CancellationToken>());
    await cityRepository.Received(1).CreateAsync(Arg.Is<City>(city => city.Name == "Lisboa" && city.Country == "PT"), Arg.Any<CancellationToken>());
    await cityRepository.Received(1).CreateAsync(Arg.Is<City>(city => city.Name == "Tóquio" && city.Country == "JP"), Arg.Any<CancellationToken>());
}
```

(`DemoOptions` com `set` em `Enabled` para o teste desligar; as demais propriedades `init`.)

- [x] **Step 2: Rodar e ver falhar** — erro de compilação.

- [x] **Step 3: Implementação**

`DemoOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Application.Options;

public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public bool Enabled { get; set; }

    [Required(ErrorMessage = "Demo:Email não configurado.")]
    [EmailAddress(ErrorMessage = "Demo:Email deve ser um e-mail válido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Demo:Name não configurado.")]
    public string Name { get; init; } = string.Empty;

    public List<DemoCity> Cities { get; init; } = [];
}

public sealed class DemoCity
{
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}
```

`AuthAppService` recebe `ICityRepository cityRepository` e `IOptions<DemoOptions> demoOptions`:

```csharp
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
```

Registrar as options em `Infrastructure/DependencyInjection.cs` como as demais:

```csharp
services.AddOptions<DemoOptions>()
    .BindConfiguration(DemoOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`appsettings.json`:

```json
"Demo": {
  "Enabled": false,
  "Email": "demo@climato.dev",
  "Name": "Visitante",
  "Cities": [
    { "Name": "São Paulo", "Country": "BR" },
    { "Name": "Cuiabá", "Country": "BR" },
    { "Name": "Lisboa", "Country": "PT" },
    { "Name": "Tóquio", "Country": "JP" }
  ]
}
```

`AuthController`:

```csharp
/// <summary>Entra com o usuário de demonstração (quando habilitado).</summary>
/// <response code="200">Autenticado como demonstração.</response>
/// <response code="404">Demonstração desativada.</response>
[HttpPost("demo")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<AuthDto>> Demo(CancellationToken cancellationToken)
{
    var auth = await appService.LoginAsDemoAsync(cancellationToken);

    if (auth == null)
    {
        return Problem(statusCode: StatusCodes.Status404NotFound, title: "Demonstração desativada.");
    }

    return Ok(auth);
}
```

`docker-compose.yml`: no `gateway`, `Demo__Enabled: "true"`; no `web.build.args`, `VITE_DEMO_ENABLED: "true"`.
`web/Dockerfile`: `ARG VITE_DEMO_ENABLED` e `ENV VITE_DEMO_ENABLED=$VITE_DEMO_ENABLED` junto do `VITE_API_URL`.
`web/.env.example`: `VITE_DEMO_ENABLED=false`.

Run: `dotnet test --solution Climato.slnx` → verdes.

- [x] **Step 4: Web — teste vermelho**

`web/src/services/auth.test.js`:

```js
import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { loginAsDemo } from "./auth";

vi.mock("./client", () => ({ default: { post: vi.fn() } }));

describe("loginAsDemo", () => {
  it("chama /auth/demo e devolve a sessão", async () => {
    const auth = { token: "t", expiresAt: "2026-11-08T12:00:00Z", user: { name: "Visitante" } };
    client.post.mockResolvedValue({ data: auth });

    const result = await loginAsDemo();

    expect(client.post).toHaveBeenCalledWith("/auth/demo");
    expect(result).toEqual(auth);
  });
});
```

Run: `npm --prefix web test` → FAIL.

- [x] **Step 5: Web — implementação**

`auth.js`:

```js
export const loginAsDemo = async () => {
  const response = await client.post("/auth/demo");
  return response.data;
};
```

`SignIn.jsx`: `const isDemoEnabled = import.meta.env.VITE_DEMO_ENABLED === "true";` no módulo; estado `isDemoLoading`; handler:

```jsx
const handleDemo = async () => {
  setIsDemoLoading(true);
  try {
    signIn(await loginAsDemo());
    navigate("/p", { replace: true });
  } catch (err) {
    notifyError(err, "Não foi possível entrar como demonstração.");
  } finally {
    setIsDemoLoading(false);
  }
};
```

Depois do botão "Entrar", dentro do `Form`:

```jsx
{isDemoEnabled && (
  <Button block onClick={handleDemo} loading={isDemoLoading} className={styles.demo}>
    Entrar como demonstração
  </Button>
)}
```

`Auth.module.css`: `.demo { margin-top: var(--ant-margin-sm); }`.

Run: `npm --prefix web test`, `npm --prefix web run lint` → verdes.

- [x] **Step 6: Conferência manual** — `Demo__Enabled=true` no Gateway local e `VITE_DEMO_ENABLED=true` no `web/.env`: botão entra e mostra as 4 cidades; excluir a conta pelo menu e clicar de novo → recriado. Com `Demo__Enabled=false`: `POST /auth/demo` → 404.

- [x] **Step 7: `/revisar-padrao` e commit**

```bash
git add src/Gateway tests/Climato.Gateway.UnitTests web docker-compose.yml
git commit -m "Adiciona o usuário de demonstração com entrada pelo login"
```

---

### Task 5: 400 em português

**Files:**
- Create: `src/Gateway/Climato.Gateway.Api/Errors/InvalidRequest.cs`
- Modify: `src/Gateway/Climato.Gateway.Api/DependencyInjection.cs`
- Create test: `tests/Climato.Gateway.UnitTests/Api/Errors/InvalidRequestTests.cs`

**Interfaces:**
- Produces: `InvalidRequest.Sanitize(ModelStateDictionary modelState, IReadOnlyCollection<string> bodyParameterNames) → ModelStateDictionary`.

- [x] **Step 1: Testes vermelhos**

```csharp
using Climato.Gateway.Api.Errors;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Climato.Gateway.UnitTests.Api.Errors;

public class InvalidRequestTests
{
    [Fact]
    public void Sanitize_ErroDeJson_ViraJsonInvalidoNoCorpo()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("$.email", "The JSON value could not be converted to System.String. Path: $.email");
        modelState.AddModelError("dto", "The dto field is required.");

        var result = InvalidRequest.Sanitize(modelState, ["dto"]);

        result.Keys.ShouldBe(["body"]);
        result["body"]!.Errors.Select(e => e.ErrorMessage).ShouldBe(["JSON inválido."]);
    }

    [Fact]
    public void Sanitize_ValorDeRotaOuQuery_ViraValorInvalidoNaMesmaChave()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("id", "The value 'abc' is not valid.");

        var result = InvalidRequest.Sanitize(modelState, []);

        result["id"]!.Errors.Select(e => e.ErrorMessage).ShouldBe(["Valor inválido."]);
    }
}
```

- [x] **Step 2: Rodar e ver falhar** — erro de compilação.

- [x] **Step 3: Implementação**

```csharp
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Climato.Gateway.Api.Errors;

public static class InvalidRequest
{
    private const string BodyKey = "body";

    public static ModelStateDictionary Sanitize(ModelStateDictionary modelState, IReadOnlyCollection<string> bodyParameterNames)
    {
        var result = new ModelStateDictionary();

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            if (key.Length == 0 || key.StartsWith('$') || bodyParameterNames.Contains(key))
            {
                if (!result.ContainsKey(BodyKey))
                {
                    result.AddModelError(BodyKey, "JSON inválido.");
                }

                continue;
            }

            result.AddModelError(key, "Valor inválido.");
        }

        return result;
    }
}
```

Em `AddGatewayApi`, a factory passa a sanitizar e o JSON deixa de expor exceções:

```csharp
services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false)
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var bodyParameters = context.ActionDescriptor.Parameters
                .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
                .Select(parameter => parameter.Name)
                .ToList();
            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(
                context.HttpContext,
                InvalidRequest.Sanitize(context.ModelState, bodyParameters),
                StatusCodes.Status400BadRequest,
                "Dados inválidos.");

            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        };
    });
```

(Com `[ApiController]`, parâmetros complexos têm `BindingSource.Body` inferido já no `ActionDescriptor`; se a conferência manual mostrar a chave `dto` sem sanitizar, ler a origem em `ControllerParameterDescriptor` e ajustar.)

- [x] **Step 4: Rodar e ver passar** — `dotnet test --solution Climato.slnx`.

- [x] **Step 5: Conferência manual**

```bash
curl -s -X POST http://localhost:5001/auth/login -H "Content-Type: application/json" -d '{"email": 1'
curl -s -X POST http://localhost:5001/auth/login -H "Content-Type: application/json" -d '{"email": 1, "password": "x"}'
```

Expected: `400` com `errors: { "body": ["JSON inválido."] }`, sem texto em inglês nem nome de tipo.

- [x] **Step 6: `/revisar-padrao` e commit** — `git commit -m "Troca as mensagens do 400 automático por textos em português"`.

---

### Task 6: `traceparent` do cliente ignorado

**Files:**
- Create: `src/Gateway/Climato.Gateway.Api/Observability/IncomingTraceContextBlocker.cs`
- Modify: `src/Gateway/Climato.Gateway.Api/Observability/ObservabilityExtensions.cs`
- Create test: `tests/Climato.Gateway.UnitTests/Api/Observability/IncomingTraceContextBlockerTests.cs`

**Interfaces:**
- Produces: `IncomingTraceContextBlocker : DistributedContextPropagator` (extrai nada; injeta como o padrão).

- [x] **Step 1: Testes vermelhos**

```csharp
using System.Diagnostics;
using Climato.Gateway.Api.Observability;

namespace Climato.Gateway.UnitTests.Api.Observability;

public class IncomingTraceContextBlockerTests
{
    private readonly IncomingTraceContextBlocker propagator = new();

    [Fact]
    public void ExtractTraceIdAndState_IgnoraOTraceparentRecebido()
    {
        var headers = new Dictionary<string, string> { ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" };

        propagator.ExtractTraceIdAndState(headers, Getter, out var traceId, out var traceState);

        traceId.ShouldBeNull();
        traceState.ShouldBeNull();
    }

    [Fact]
    public void Inject_PropagaAAtividadeAtual()
    {
        using var activity = new Activity("teste").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, string>();

        propagator.Inject(activity, headers, (carrier, name, value) => ((Dictionary<string, string>)carrier!)[name] = value);

        headers["traceparent"].ShouldContain(activity.TraceId.ToHexString());
    }

    private static void Getter(object? carrier, string name, out string? value, out IEnumerable<string>? values)
    {
        ((Dictionary<string, string>)carrier!).TryGetValue(name, out value);
        values = null;
    }
}
```

- [x] **Step 2: Rodar e ver falhar** — erro de compilação.

- [x] **Step 3: Implementação**

```csharp
using System.Diagnostics;

namespace Climato.Gateway.Api.Observability;

public sealed class IncomingTraceContextBlocker : DistributedContextPropagator
{
    private readonly DistributedContextPropagator inner = CreateDefaultPropagator();

    public override IReadOnlyCollection<string> Fields => inner.Fields;

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
    {
        inner.Inject(activity, carrier, setter);
    }

    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
    {
        traceId = null;
        traceState = null;
    }

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
    {
        return null;
    }
}
```

Em `AddObservability`:

```csharp
builder.Services.AddSingleton<DistributedContextPropagator, IncomingTraceContextBlocker>();
Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());
```

O primeiro faz o hosting do ASP.NET Core criar sempre um trace raiz. O segundo impede a instrumentação do OpenTelemetry de reextrair o `traceparent` (ela só reextrai quando o propagador padrão **não** é `TraceContextPropagator`; o padrão do SDK é composto com Baggage). A injeção no `HttpClient` usa `DistributedContextPropagator.Current` (estático), que não muda. Usings: `System.Diagnostics`, `OpenTelemetry`, `OpenTelemetry.Context.Propagation`.

- [x] **Step 4: Rodar e ver passar** — `dotnet test --solution Climato.slnx`.

- [x] **Step 5: Conferência manual**

```bash
curl -s -o /dev/null -D - http://localhost:5001/cities -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00" | grep -i x-correlation-id
```

Expected: `X-Correlation-ID` diferente de `0af7651916cd43dd8448eb211c80319c`. Com um token válido em `/weathers/Lima`, o log do Provider mostra o mesmo TraceId do Gateway (propagação interna mantida).

- [x] **Step 6: `/revisar-padrao` e commit** — `git commit -m "Ignora o traceparent recebido do cliente no Gateway"`.

---

### Task 7: Health check do Provider e 503 rápido

**Files:**
- Modify: `src/Gateway/Climato.Gateway.Infrastructure/DependencyInjection.cs`
- Modify: `src/Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs`
- Test: `tests/Climato.Gateway.UnitTests/Infrastructure/Http/WeatherProviderClientTests.cs`

**Interfaces:**
- Consumes: `WeatherProviderClient` e o teste da Task 2.

- [x] **Step 1: Teste vermelho** (timeout de conexão vira `ProviderUnavailableException`)

Em `WeatherProviderClientTests`:

```csharp
[Fact]
public async Task GetWeatherAsync_TimeoutDeConexao_LancaProviderUnavailable()
{
    var handler = new ThrowingHandler(new TaskCanceledException("connect", new TimeoutException()));
    var client = new WeatherProviderClient(new HttpClient(handler) { BaseAddress = new Uri("http://provider/") });

    await Should.ThrowAsync<ProviderUnavailableException>(() => client.GetWeatherAsync("Lima", true, cancellationToken));
}

[Fact]
public async Task GetWeatherAsync_RequisicaoCanceladaPeloCliente_PropagaOCancelamento()
{
    using var cts = new CancellationTokenSource();
    await cts.CancelAsync();
    var handler = new ThrowingHandler(new TaskCanceledException("abortada", null, cts.Token));
    var client = new WeatherProviderClient(new HttpClient(handler) { BaseAddress = new Uri("http://provider/") });

    await Should.ThrowAsync<OperationCanceledException>(() => client.GetWeatherAsync("Lima", true, cts.Token));
}

private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromException<HttpResponseMessage>(exception);
    }
}
```

(Usings: `Climato.Gateway.Application.Exceptions`.)

- [x] **Step 2: Rodar e ver falhar** — o primeiro teste falha (`TaskCanceledException` escapa).

- [x] **Step 3: Implementação**

`WeatherProviderClient.GetAsync`, o `catch` passa a incluir o timeout de conexão (cancelamento que não veio de quem chamou):

```csharp
catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException
    || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
{
    throw new ProviderUnavailableException("Falha ao chamar o Provider.", exception);
}
```

`Infrastructure/DependencyInjection.cs`:

```csharp
private static readonly TimeSpan healthCheckTimeout = TimeSpan.FromSeconds(3);
private static readonly TimeSpan providerHealthCheckTimeout = TimeSpan.FromSeconds(5);
private static readonly TimeSpan providerConnectTimeout = TimeSpan.FromSeconds(2);
```

Client tipado:

```csharp
services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>(ConfigureProviderClient)
    .ConfigurePrimaryHttpMessageHandler(CreateProviderHandler)
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(25);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.Retry.MaxRetryAttempts = 1;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.ShouldHandle = args => ValueTask.FromResult(IsTransportFailure(args.Outcome.Exception, args.Context.CancellationToken));
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    });
```

```csharp
private static SocketsHttpHandler CreateProviderHandler()
{
    return new SocketsHttpHandler { ConnectTimeout = providerConnectTimeout };
}

private static bool IsTransportFailure(Exception? exception, CancellationToken cancellationToken)
{
    return exception is HttpRequestException
        || (exception is TaskCanceledException { InnerException: TimeoutException } && !cancellationToken.IsCancellationRequested);
}
```

(O timeout por tentativa do Polly lança `TimeoutRejectedException`, que não é `TaskCanceledException`: resposta lenta continua sem retentativa, como hoje.)

Client do health check: `.ConfigurePrimaryHttpMessageHandler(CreateProviderHandler)` e `client.Timeout = TimeSpan.FromSeconds(4)`. No `PostConfigure`:

```csharp
foreach (var registration in options.Registrations)
{
    registration.Timeout = registration.Name == "provider" ? providerHealthCheckTimeout : healthCheckTimeout;
}
```

- [x] **Step 4: Rodar e ver passar** — `dotnet test --solution Climato.slnx`.

- [x] **Step 5: Conferência manual**
  - Gateway local com o Provider parado: `curl -w "%{time_total}\n"` em `/weathers/Lima` (com token) → 503 em ≤ 5 s.
  - `Provider__BaseUrl=http://10.255.255.1:8080/` → 503 em ≤ 5 s.
  - Compose sem o `web` (`docker compose up -d gateway`) com `docker compose pause redis` → `curl localhost:5001/health/ready` 200 `Healthy`; `docker compose unpause redis`.

- [x] **Step 6: `/revisar-padrao` e commit** — `git commit -m "Limita a conexão com o Provider e ajusta o health check dele no Gateway"`.

---

### Task 8: Documentação, verificação e fechamento da 08a

**Files:**
- Modify: `docs/architecture/backend.md`, `docs/architecture/frontend.md`, `docs/architecture/review-checklist.md`, `docs/architecture/system-design.md`, `docs/runbook.md`, `docs/project-overview.md`, `.env.example` (se precisar), este plano (notas de execução).

- [x] **Step 1: `backend.md`**
  - Comunicação HTTP: `ConnectTimeout` de 2 s no client do Provider, retentativa com 500 ms, timeout de conexão tratado como falha de transporte.
  - Health checks: Provider no Gateway com 5 s (client 4 s); demais 3 s.
  - Observabilidade: o Gateway ignora `traceparent` de entrada (`IncomingTraceContextBlocker` + `TraceContextPropagator`).
  - Nova subseção **Rate limiting**: políticas `auth` (IP) e `api` (usuário), `[EnableRateLimiting]` em todo controller, 429 com `Retry-After`, limites em `RateLimiting:*`; atrás de proxy depende do `ForwardedHeaders` (08b).
  - Erros: 400 de binding sanitizado (`InvalidRequest`), sem mensagens do System.Text.Json.
  - Decisões: `GET` não grava; histórico via `POST /cities` idempotente; usuário demo (`Demo:*`, `POST /auth/demo`, criado sob demanda).
- [x] **Step 2: `frontend.md`** — o detalhe registra a cidade (`addCity`) quando o clima carrega; cards usam `useWeather(nome, { image: false })`; botão de demonstração com `VITE_DEMO_ENABLED`.
- [x] **Step 3: `review-checklist.md`** — novas linhas: `BE-31 🔴 08 GET com efeito colateral` (GET que grava/alterar estado) e `BE-32 🔴 08 Rota sem rate limit` (controller sem `[EnableRateLimiting]`).
- [x] **Step 4: `system-design.md`** — fluxo "Consulta de clima" sem gravação e novo passo de registro no histórico; menção ao usuário demo e ao rate limit.
- [x] **Step 5: `runbook.md`** — `Demo__Enabled`, `VITE_DEMO_ENABLED`, `RateLimiting__*` (como aumentar em desenvolvimento).
- [x] **Step 6: `project-overview.md`** — remover as pendências `[Fase 08]` resolvidas (rate limiting, 400, health, `traceparent`, Provider fora, efeito colateral do `GET /weathers`, custo dos cards); adicionar `[Fase 08] ForwardedHeaders no Gateway atrás do proxy da hospedagem (IP real para o rate limit)`; Estado Atual e Fase 08 com "08a concluída (AAAA-MM-DD); 08b pendente" (a fase só ganha ✅ na 08b).
- [x] **Step 7: Verificação completa**

```bash
dotnet build Climato.slnx
dotnet test --solution Climato.slnx
npm --prefix web run lint
npm --prefix web test
npm --prefix web run build
docker compose build
```

(`docker compose build` com `.env` temporário de build, apagado em seguida, como na 07.)

- [x] **Step 8: Notas de execução** — registrar no fim deste plano desvios e o que foi conferido manualmente.
- [x] **Step 9: `/revisar-padrao`, `/requesting-code-review` e commit final**

```bash
git add docs .env.example
git commit -m "Documenta o endurecimento e fecha a Fase 08a"
```

---

## Notas de execução (2026-11-08)

**Desvios do plano**
- Task 2: o `CapturingHandler` do `WeatherProviderClientTests` devolve 404 em vez de um JSON parcial (o `WeatherDto` tem propriedades `required` e o JSON do plano virava `ProviderUnavailableException`); o teste só confere a URL.
- Task 3: `RateLimitingOptions` registrada com `AddOptions().BindConfiguration().ValidateDataAnnotations().ValidateOnStart()` e `[Range]` (BE-20), lida por `IOptions` dentro das políticas; `RateLimitPolicies.IpPartitionKey` extraído para a política `auth`.
- Task 4: design ajustado para o usuário demo sob demanda (sem seeder).
- Task 5: corpo vazio gera a chave `""` no `ModelState`; o `Sanitize` também a trata como corpo (teste `Sanitize_CorpoVazio_ViraSoJsonInvalido`).
- Task 6: teste extra de `ExtractBaggage`.

**Conferido manualmente** (Gateway/Provider com `dotnet run`, web com `npm run dev`, Postgres local)
- Home não grava histórico; detalhe dispara `POST /cities` (201) e a cidade aparece na Home; `POST` repetido → 200; `name` vazio → 400 em português; sem token → 401.
- Cards pedem `?image=false` (`imageUrl: null`); o detalhe mantém a foto.
- 10 logins → 401, 11º → 429 com `Retry-After` e título em português; `/health/live` fora do limite.
- Demo desligada → 404; ligada → botão entra com as 4 cidades; após `DELETE /users/me`, novo acesso recria usuário e cidades.
- 400: JSON malformado, tipo errado e corpo vazio → só `body: ["JSON inválido."]`; `id` inválido na rota → "Valor inválido.".
- `traceparent` próprio do cliente → `X-Correlation-ID` diferente; o Provider loga o mesmo TraceId do Gateway.
- Provider fora: porta recusada 9,7 s → 4,4 s; host inalcançável (`10.255.255.1`) → 4,8 s; `/health/ready` 503 em 2 s.
- Compose (`docker compose up -d gateway`, sem o `web`) com `docker compose pause redis`: em regime `Healthy` 200 em ~3 s. Nos primeiros segundos após a pausa o check de Redis do Provider passa dos 3 s (4–8 s, `RedisTimeoutException`) e o Gateway fica Unhealthy por esse intervalo — registrado como pendência `[Backlog]` no `project-overview.md` (correção no Provider, fora desta fase).

**Verificação final:** `dotnet build` (0 avisos), `dotnet test` 126/126, `npm run lint`, `npm test` 50/50, `npm run build` e `docker compose build` com `.env` temporário (apagado em seguida).

**Correções depois do code review** (os trechos de código das tasks acima ficaram como planejados; o código atual reflete estas correções)
- `POST /auth/register` recusa `Demo:Email` (409), com a demo ligada ou não: depois de um `DELETE /users/me` da demo, ninguém registra o e-mail com senha própria e passa a receber os visitantes do botão.
- Política `api`: o usuário de demonstração é particionado por `sub` + IP (todos os visitantes usam o mesmo `sub`); `ApiPermitLimit` padrão 120/min, folga para a Home (uma chamada por card). O card de temperatura já mostra "—" sem `message.error` quando recebe 429.
- `POST /cities` recebe só `{ name }` (≤ 100): o Gateway resolve no Provider (`GetWeatherAsync(nome, includeImage: false)`) e grava o nome e o país devolvidos; inexistente → 404. Texto livre não entra no histórico compartilhado da demo. Web: `addCity(weather.city)`.
- Ficaram como pendência `[Backlog]` no `project-overview.md`: corrida na criação da demo, duplicata do `POST /cities` simultâneo (StrictMode) junto do índice único e o `required` implícito antes do FluentValidation.
- Conferido: cadastro com `demo@climato.dev` → 409; `POST /cities` com texto inventado → 404; "sao paulo" → 201 "São Paulo"/BR; repetido → 200; detalhe no navegador → 201 e a cidade aparece na Home.
