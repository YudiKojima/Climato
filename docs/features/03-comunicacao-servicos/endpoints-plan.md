# Fase 03b — Novos endpoints: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** expor pelo Gateway a previsão de 5 dias e a busca de cidades, implementadas no Provider sobre um `OpenWeatherClient` único e um cache genérico, conforme o [design aprovado](endpoints-design.md).

**Arquitetura:** primeiro o Provider sem mudança de comportamento (extrair `OpenWeatherClient` e trocar `WeatherCache` por `ProviderCache`), depois os dois endpoints novos no Provider, depois o Gateway (client, AppServices, validator e controllers), por fim a documentação e o fechamento da Fase 03. Cada tarefa compila sozinha e vira um commit.

**Stack:** .NET 10 · System.Text.Json (`System.Net.Http.Json`) · Microsoft.Extensions.Http.Resilience · IDistributedCache · FluentValidation. Nenhum pacote novo.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) antes de editar. O design vale mais que este plano em caso de conflito.
- **Sem projeto de testes** (Fase 04). Cada tarefa termina com `dotnet build Climato.slnx` sem erros e sem avisos `CS86xx`. Verificação funcional na Tarefa 7.
- **Sem mudança no front** (`web/`): autocomplete e tela de previsão são da Fase 07.
- **Commits:** um por tarefa (1–6), com as mensagens indicadas. A aprovação deste plano é a autorização explícita para esses commits (mesma exceção das Fases 01–03a à regra "nunca commitar durante a fase"). Antes de cada commit, `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/novos-endpoints`, a partir do `origin/master` com o PR #10 (03a) mergeado.
- **Nunca** gravar chaves (OpenWeather, Pexels, `Jwt:SecretKey`) em arquivo versionado.
- Fora do escopo: front (07); testes (04); busca por `lat`/`lon`; previsão horária; mudança de timeouts.
- Código C#: chaves em todo `if`/`else`/`foreach`; campos privados `camelCase` sem `_`; namespace file-scoped; mensagens em português; `CancellationToken` sem default nas interfaces; sem comentários do "o quê"; `using` em todo `HttpResponseMessage`; query com `Uri.EscapeDataString`.

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| `units=metric&lang=pt_br` nos três endpoints | Só em `weather` e `forecast`; `geo/1.0/direct` usa `q`, `limit=5` e `appid` | A Geocoding API não aceita esses parâmetros; o nome em português vem de `local_names.pt` |
| `LocationSearchQuery.Query` | `string?`, validado com `NotEmpty` antes do tamanho | Com `string` não anulável, o `[ApiController]` aplica `[Required]` implícito e devolve a mensagem em inglês antes do validator |
| Docs de `OpenWeatherClient`/`ProviderCache` e BE-28 na Tarefa 5 | Feitos no commit da Tarefa 1 (itens **Comunicação HTTP** e **Cache** do Passo 1 e o Passo 3 da Tarefa 5) | GE-01 no `/revisar-padrao` da Tarefa 1: o `backend.md` citava o `IWeatherCache` removido |
| Sufixo dos DTOs de entrada (`backend.md`) | Novo sufixo `Query` documentado em **DTOs** e incluído no BE-22, no commit da Tarefa 4 | GE-01 no `/revisar-padrao` da Tarefa 4: `LocationSearchQuery` não cabia em `PostDto`/`PutDto` |
| Parâmetro `LocationSearchQuery query` no `LocationController` do Gateway | Renomeado para `request` | Verificação manual (Tarefa 7): com o nome igual à chave `?query=`, o binder procurava `query.Query` e toda busca válida virava 400 |
| Mensagem do 400 da busca no Gateway | Título "Dados inválidos." (padrão do `ValidationFilter`) com a mensagem do design em `errors.Query` | Mesmo formato de toda validação do Gateway |

## Mapa de arquivos

Caminhos relativos a `src/`.

| Arquivo | Ação | Tarefa |
|---|---|---|
| `Provider/Climato.Provider.Application/Contracts/IOpenWeatherClient.cs`, `Services/OpenWeatherClient.cs` | Criar | 1, 2, 3 |
| `Provider/Climato.Provider.Application/Contracts/IProviderCache.cs`, `Services/ProviderCache.cs` | Criar | 1 |
| `Provider/Climato.Provider.Application/Contracts/IWeatherCache.cs`, `Services/WeatherCache.cs` | Remover | 1 |
| `Provider/Climato.Provider.Application/Services/WeatherAppService.cs` | Reescrever | 1 |
| `Provider/Climato.Provider.Application/DependencyInjection.cs` | Modificar | 1, 2, 3 |
| `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherForecastResponse.cs` | Criar | 2 |
| `Provider/Climato.Provider.Domain/Weathers/{Forecast,ForecastDay}.cs` | Criar | 2 |
| `Provider/Climato.Provider.Application/Contracts/IForecastAppService.cs`, `Services/ForecastAppService.cs` | Criar | 2 |
| `Provider/Climato.Provider.Api/Controllers/ForecastController.cs` | Criar | 2 |
| `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherLocationResponse.cs` | Criar | 3 |
| `Provider/Climato.Provider.Domain/Locations/Location.cs` | Criar | 3 |
| `Provider/Climato.Provider.Application/Contracts/ILocationAppService.cs`, `Services/LocationAppService.cs` | Criar | 3 |
| `Provider/Climato.Provider.Api/Controllers/LocationController.cs` | Criar | 3 |
| `Gateway/Climato.Gateway.Application/Model/Weathers/{ForecastDto,ForecastDayDto}.cs` | Criar | 4 |
| `Gateway/Climato.Gateway.Application/Model/Locations/{LocationDto,LocationSearchQuery}.cs` | Criar | 4 |
| `Gateway/Climato.Gateway.Application/Validators/LocationSearchQueryValidator.cs` | Criar | 4 |
| `Gateway/Climato.Gateway.Application/Contracts/{IWeatherProviderClient,IWeatherAppService}.cs` | Modificar | 4 |
| `Gateway/Climato.Gateway.Application/Contracts/ILocationAppService.cs`, `Services/LocationAppService.cs` | Criar | 4 |
| `Gateway/Climato.Gateway.Application/Services/WeatherAppService.cs`, `DependencyInjection.cs` | Modificar | 4 |
| `Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs` | Reescrever | 4 |
| `Gateway/Climato.Gateway.Api/Controllers/{ForecastController,LocationController}.cs` | Criar | 4 |
| `../docs/architecture/{backend,system-design,review-checklist,frontend}.md` | Modificar | 5 |
| `../docs/project-overview.md` | Modificar | 6 |

---

### Tarefa 0: Preparar a branch

- [x] **Passo 1:** branch `dev/novos-endpoints` criada a partir do `origin/master` (`git log --oneline -1` mostra `Merge pull request #10 from YudiKojima/dev/contrato-robustez`), sem upstream. Os rascunhos antigos não rastreados da 03a foram movidos para o scratchpad da sessão.

---

### Tarefa 1: Provider — `OpenWeatherClient` e `ProviderCache` (sem mudança de comportamento)

**Files:**
- Create: `Provider/Climato.Provider.Application/Contracts/IOpenWeatherClient.cs`, `Services/OpenWeatherClient.cs`, `Contracts/IProviderCache.cs`, `Services/ProviderCache.cs`
- Modify: `Services/WeatherAppService.cs`, `DependencyInjection.cs`
- Delete: `Contracts/IWeatherCache.cs`, `Services/WeatherCache.cs`

**Interfaces:**
- Produces: `IOpenWeatherClient.GetCurrentAsync(string cityName, CancellationToken) : Task<OpenWeatherCurrentResponse?>`; `IProviderCache.GetAsync<T>(string prefix, string text, CancellationToken) : Task<T?> where T : class`; `IProviderCache.SetAsync<T>(string prefix, string text, T value, TimeSpan duration, CancellationToken) : Task where T : class`; `OpenWeatherClient` com `GetAsync<T>(string url, CancellationToken) : Task<T?>` privado.

- [x] **Passo 1: `Contracts/IOpenWeatherClient.cs` (criar)**

```csharp
using Climato.Provider.Application.Model.OpenWeather;

namespace Climato.Provider.Application.Contracts;

public interface IOpenWeatherClient
{
    Task<OpenWeatherCurrentResponse?> GetCurrentAsync(string cityName, CancellationToken cancellationToken);
}
```

- [x] **Passo 2: `Services/OpenWeatherClient.cs` (criar)** — move o `GetCurrentAsync` de `WeatherAppService` e generaliza o tratamento de erro.

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Exceptions;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Options;
using Microsoft.Extensions.Options;
using Polly;

namespace Climato.Provider.Application.Services;

public sealed class OpenWeatherClient : IOpenWeatherClient
{
    private const string ServiceName = "OpenWeather";

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient client;
    private readonly string apiKey;

    public OpenWeatherClient(HttpClient httpClient, IOptions<OpenWeatherOptions> options)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
    }

    public Task<OpenWeatherCurrentResponse?> GetCurrentAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";
        return GetAsync<OpenWeatherCurrentResponse>(url, cancellationToken);
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceUnavailableException(ServiceName, $"status {(int)response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ExternalServiceUnavailableException(ServiceName, "resposta vazia");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ExternalServiceUnavailableException(ServiceName, "falha na chamada", exception);
        }
    }
}
```

- [x] **Passo 3: `Contracts/IProviderCache.cs` (criar)**

```csharp
namespace Climato.Provider.Application.Contracts;

public interface IProviderCache
{
    Task<T?> GetAsync<T>(string prefix, string text, CancellationToken cancellationToken) where T : class;
    Task SetAsync<T>(string prefix, string text, T value, TimeSpan duration, CancellationToken cancellationToken) where T : class;
}
```

- [x] **Passo 4: `Services/ProviderCache.cs` (criar)** — mesma lógica do `WeatherCache`, com prefixo e duração por chamada.

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Climato.Provider.Application.Services;

public sealed class ProviderCache : IProviderCache
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache cache;
    private readonly ILogger<ProviderCache> logger;

    public ProviderCache(IDistributedCache cache, ILogger<ProviderCache> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<T?> GetAsync<T>(string prefix, string text, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var bytes = await cache.GetAsync(CreateKey(prefix, text), cancellationToken);
            return bytes == null ? null : JsonSerializer.Deserialize<T>(bytes, jsonOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao ler o cache {Prefix} de {Text}; seguindo sem cache.", prefix, text);
            return null;
        }
    }

    public async Task SetAsync<T>(string prefix, string text, T value, TimeSpan duration, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, jsonOptions);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };
            await cache.SetAsync(CreateKey(prefix, text), bytes, options, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao gravar o cache {Prefix} de {Text}; seguindo sem cache.", prefix, text);
        }
    }

    private static string CreateKey(string prefix, string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var decomposed = string.Join(' ', words).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return $"{prefix}:{builder.ToString().Normalize(NormalizationForm.FormC)}";
    }
}
```

- [x] **Passo 5: remover** `Contracts/IWeatherCache.cs` e `Services/WeatherCache.cs`.

- [x] **Passo 6: `Services/WeatherAppService.cs` (reescrever)** — sem `HttpClient`, `apiKey` nem `jsonOptions`; `ToWeather` igual ao atual.

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Services;

public sealed class WeatherAppService : IWeatherAppService
{
    private const string CachePrefix = "clima";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(10);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly ICityImageAppService cityImageAppService;
    private readonly IProviderCache cache;

    public WeatherAppService(IOpenWeatherClient openWeatherClient, ICityImageAppService cityImageAppService, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
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

        var current = await openWeatherClient.GetCurrentAsync(cityName, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = await cityImageAppService.GetImageUrlAsync(current.Name, cancellationToken);
        var weather = ToWeather(current, imageUrl);

        await cache.SetAsync(CachePrefix, cityName, weather, cacheDuration, cancellationToken);
        return weather;
    }

    private static Weather ToWeather(OpenWeatherCurrentResponse current, string? imageUrl)
    {
        var offset = TimeSpan.FromSeconds(current.Timezone);
        var condition = current.Weather[0];

        return new Weather
        {
            City = current.Name,
            Country = current.Sys.Country,
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

- [x] **Passo 7: `DependencyInjection.cs`** — trocar o registro do cache e o typed client da OpenWeather:

```csharp
        services.AddSingleton<IProviderCache, ProviderCache>();

        services.AddHttpClient<IOpenWeatherClient, OpenWeatherClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.Retry.MaxRetryAttempts = 2;
            });

        services.AddScoped<IWeatherAppService, WeatherAppService>();
```

(no lugar de `services.AddSingleton<IWeatherCache, WeatherCache>();` e de `services.AddHttpClient<IWeatherAppService, WeatherAppService>(...)...`). O registro do Pexels não muda.

- [x] **Passo 8: Build** — `dotnet build Climato.slnx` → sem erros e sem avisos `CS86xx`. `grep -rn "WeatherCache" src/` → nenhum resultado.

- [x] **Passo 9: Commit** (após `/revisar-padrao`)

```bash
git add src/Provider/Climato.Provider.Application
git commit -m "Extrai OpenWeatherClient e troca WeatherCache por cache genérico no Provider

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 2: Provider — previsão de 5 dias

**Files:**
- Create: `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherForecastResponse.cs`, `Provider/Climato.Provider.Domain/Weathers/Forecast.cs`, `Weathers/ForecastDay.cs`, `Provider/Climato.Provider.Application/Contracts/IForecastAppService.cs`, `Services/ForecastAppService.cs`, `Provider/Climato.Provider.Api/Controllers/ForecastController.cs`
- Modify: `Contracts/IOpenWeatherClient.cs`, `Services/OpenWeatherClient.cs`, `DependencyInjection.cs`

**Interfaces:**
- Consumes: `IOpenWeatherClient`, `IProviderCache` (Tarefa 1).
- Produces: `IOpenWeatherClient.GetForecastAsync(string cityName, CancellationToken) : Task<OpenWeatherForecastResponse?>`; `IForecastAppService.GetByCityAsync(string cityName, CancellationToken) : Task<Forecast?>`; `GET /forecasts?cityName=` com o JSON da seção 1 do design.

- [x] **Passo 1: `Model/OpenWeather/OpenWeatherForecastResponse.cs` (criar)** — reaproveita `OpenWeatherCondition` do clima atual.

```csharp
namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherForecastResponse(IReadOnlyList<OpenWeatherForecastItem> List, OpenWeatherForecastCity City);

public sealed record OpenWeatherForecastItem(
    long Dt,
    OpenWeatherForecastMain Main,
    IReadOnlyList<OpenWeatherCondition> Weather,
    OpenWeatherForecastWind Wind,
    double Pop);

public sealed record OpenWeatherForecastMain(double TempMin, double TempMax);

public sealed record OpenWeatherForecastWind(double Speed);

public sealed record OpenWeatherForecastCity(string Name, string Country, int Timezone);
```

- [x] **Passo 2: `Provider/Climato.Provider.Domain/Weathers/ForecastDay.cs` (criar)**

```csharp
namespace Climato.Provider.Domain.Weathers;

public sealed class ForecastDay
{
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required double TempMin { get; init; }
    public required double TempMax { get; init; }
    public required int PrecipitationProbability { get; init; }
    public required double WindSpeedKmh { get; init; }
}
```

- [x] **Passo 3: `Provider/Climato.Provider.Domain/Weathers/Forecast.cs` (criar)**

```csharp
namespace Climato.Provider.Domain.Weathers;

public sealed class Forecast
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required IReadOnlyList<ForecastDay> Days { get; init; }
}
```

- [x] **Passo 4: `IOpenWeatherClient` / `OpenWeatherClient`** — acrescentar:

```csharp
    Task<OpenWeatherForecastResponse?> GetForecastAsync(string cityName, CancellationToken cancellationToken);
```

```csharp
    public Task<OpenWeatherForecastResponse?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/forecast?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";
        return GetAsync<OpenWeatherForecastResponse>(url, cancellationToken);
    }
```

- [x] **Passo 5: `Contracts/IForecastAppService.cs` (criar)**

```csharp
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts;

public interface IForecastAppService
{
    Task<Forecast?> GetByCityAsync(string cityName, CancellationToken cancellationToken);
}
```

- [x] **Passo 6: `Services/ForecastAppService.cs` (criar)** — agrega por data local (regras da seção 1 do design).

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Services;

public sealed class ForecastAppService : IForecastAppService
{
    private const string CachePrefix = "previsao";
    private const int MaxDays = 5;

    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan midday = TimeSpan.FromHours(12);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly IProviderCache cache;

    public ForecastAppService(IOpenWeatherClient openWeatherClient, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cache = cache;
    }

    public async Task<Forecast?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<Forecast>(CachePrefix, cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var response = await openWeatherClient.GetForecastAsync(cityName, cancellationToken);

        if (response == null)
        {
            return null;
        }

        var forecast = ToForecast(response);

        await cache.SetAsync(CachePrefix, cityName, forecast, cacheDuration, cancellationToken);
        return forecast;
    }

    private static Forecast ToForecast(OpenWeatherForecastResponse response)
    {
        var offset = TimeSpan.FromSeconds(response.City.Timezone);

        var days = response.List
            .Select(item => (Item: item, Local: DateTimeOffset.FromUnixTimeSeconds(item.Dt).ToOffset(offset)))
            .GroupBy(entry => DateOnly.FromDateTime(entry.Local.DateTime))
            .OrderBy(group => group.Key)
            .Take(MaxDays)
            .Select(group => ToForecastDay(group.Key, group.ToList()))
            .ToList();

        return new Forecast
        {
            City = response.City.Name,
            Country = response.City.Country,
            Days = days
        };
    }

    private static ForecastDay ToForecastDay(DateOnly date, IReadOnlyList<(OpenWeatherForecastItem Item, DateTimeOffset Local)> entries)
    {
        var middayItem = entries.MinBy(entry => (entry.Local.TimeOfDay - midday).Duration()).Item;
        var condition = middayItem.Weather[0];

        return new ForecastDay
        {
            Date = date,
            Description = condition.Description,
            Icon = condition.Icon,
            TempMin = entries.Min(entry => entry.Item.Main.TempMin),
            TempMax = entries.Max(entry => entry.Item.Main.TempMax),
            PrecipitationProbability = (int)Math.Round(entries.Max(entry => entry.Item.Pop) * 100),
            WindSpeedKmh = Math.Round(entries.Max(entry => entry.Item.Wind.Speed) * 3.6, 1)
        };
    }
}
```

- [x] **Passo 7: `DependencyInjection.cs`** — após o registro do `IWeatherAppService`:

```csharp
        services.AddScoped<IForecastAppService, ForecastAppService>();
```

- [x] **Passo 8: `Provider/Climato.Provider.Api/Controllers/ForecastController.cs` (criar)**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Weathers;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Controllers;

[ApiController]
[Route("forecasts")]
public class ForecastController : ControllerBase
{
    private readonly IForecastAppService appService;

    public ForecastController(IForecastAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna a previsão de até 5 dias da cidade informada, agregada por dia no fuso da cidade.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Previsão diária: mínima, máxima, condição do meio-dia, chance de chuva (%) e vento máximo em km/h.</response>
    /// <response code="400">Parâmetro 'cityName' ausente.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">OpenWeather indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Forecast>> Get([FromQuery] string cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Parâmetro 'cityName' é obrigatório.");
        }

        var forecast = await appService.GetByCityAsync(cityName, cancellationToken);

        if (forecast == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(forecast);
    }
}
```

- [x] **Passo 9: Build** — `dotnet build Climato.slnx` → sem erros e sem avisos `CS86xx`.

- [x] **Passo 10: Commit** (após `/revisar-padrao`)

```bash
git add src/Provider
git commit -m "Adiciona a previsão de 5 dias no Provider

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 3: Provider — busca de cidades

**Files:**
- Create: `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherLocationResponse.cs`, `Provider/Climato.Provider.Domain/Locations/Location.cs`, `Provider/Climato.Provider.Application/Contracts/ILocationAppService.cs`, `Services/LocationAppService.cs`, `Provider/Climato.Provider.Api/Controllers/LocationController.cs`
- Modify: `Contracts/IOpenWeatherClient.cs`, `Services/OpenWeatherClient.cs`, `DependencyInjection.cs`

**Interfaces:**
- Consumes: `IOpenWeatherClient`, `IProviderCache` (Tarefa 1).
- Produces: `IOpenWeatherClient.GetLocationsAsync(string query, CancellationToken) : Task<IReadOnlyList<OpenWeatherLocationResponse>>`; `ILocationAppService.SearchAsync(string query, CancellationToken) : Task<IReadOnlyList<Location>>`; `GET /locations?query=`.

- [x] **Passo 1: `Model/OpenWeather/OpenWeatherLocationResponse.cs` (criar)** — `local_names` é dicionário de código de idioma → nome (as chaves não passam pela política snake_case).

```csharp
namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherLocationResponse(
    string Name,
    IReadOnlyDictionary<string, string>? LocalNames,
    string Country,
    string? State);
```

- [x] **Passo 2: `Provider/Climato.Provider.Domain/Locations/Location.cs` (criar)**

```csharp
namespace Climato.Provider.Domain.Locations;

public sealed class Location
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public required string Country { get; init; }
}
```

- [x] **Passo 3: `IOpenWeatherClient` / `OpenWeatherClient`** — acrescentar:

```csharp
    Task<IReadOnlyList<OpenWeatherLocationResponse>> GetLocationsAsync(string query, CancellationToken cancellationToken);
```

```csharp
    public async Task<IReadOnlyList<OpenWeatherLocationResponse>> GetLocationsAsync(string query, CancellationToken cancellationToken)
    {
        var url = $"geo/1.0/direct?q={Uri.EscapeDataString(query)}&limit=5&appid={apiKey}";
        return await GetAsync<List<OpenWeatherLocationResponse>>(url, cancellationToken) ?? [];
    }
```

- [x] **Passo 4: `Contracts/ILocationAppService.cs` (criar)**

```csharp
using Climato.Provider.Domain.Locations;

namespace Climato.Provider.Application.Contracts;

public interface ILocationAppService
{
    Task<IReadOnlyList<Location>> SearchAsync(string query, CancellationToken cancellationToken);
}
```

- [x] **Passo 5: `Services/LocationAppService.cs` (criar)** — lista vazia também vai para o cache.

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Domain.Locations;

namespace Climato.Provider.Application.Services;

public sealed class LocationAppService : ILocationAppService
{
    private const string CachePrefix = "locais";
    private const string PortugueseLanguageCode = "pt";

    private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(24);

    private readonly IOpenWeatherClient openWeatherClient;
    private readonly IProviderCache cache;

    public LocationAppService(IOpenWeatherClient openWeatherClient, IProviderCache cache)
    {
        this.openWeatherClient = openWeatherClient;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<Location>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<List<Location>>(CachePrefix, query, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var response = await openWeatherClient.GetLocationsAsync(query, cancellationToken);
        var locations = response.Select(ToLocation).ToList();

        await cache.SetAsync(CachePrefix, query, locations, cacheDuration, cancellationToken);
        return locations;
    }

    private static Location ToLocation(OpenWeatherLocationResponse location)
    {
        var name = location.LocalNames?.GetValueOrDefault(PortugueseLanguageCode) ?? location.Name;

        return new Location
        {
            Name = name,
            State = location.State,
            Country = location.Country
        };
    }
}
```

- [x] **Passo 6: `DependencyInjection.cs`** — após o registro do `IForecastAppService`:

```csharp
        services.AddScoped<ILocationAppService, LocationAppService>();
```

- [x] **Passo 7: `Provider/Climato.Provider.Api/Controllers/LocationController.cs` (criar)**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Locations;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Controllers;

[ApiController]
[Route("locations")]
public class LocationController : ControllerBase
{
    private const int MinQueryLength = 2;
    private const int MaxQueryLength = 100;

    private readonly ILocationAppService appService;

    public LocationController(ILocationAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Busca cidades pelo nome para o autocomplete (até 5 resultados).</summary>
    /// <param name="query">Parte do nome da cidade, exemplo: São Pa</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidades encontradas, com nome em português quando houver; lista vazia sem resultado.</response>
    /// <response code="400">Parâmetro 'query' com menos de 2 ou mais de 100 caracteres.</response>
    /// <response code="503">OpenWeather indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<Location>>> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? string.Empty;

        if (trimmed.Length is < MinQueryLength or > MaxQueryLength)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Parâmetro 'query' deve ter entre 2 e 100 caracteres.");
        }

        var locations = await appService.SearchAsync(trimmed, cancellationToken);
        return Ok(locations);
    }
}
```

- [x] **Passo 8: Build** — `dotnet build Climato.slnx` → sem erros e sem avisos `CS86xx`.

- [x] **Passo 9: Commit** (após `/revisar-padrao`)

```bash
git add src/Provider
git commit -m "Adiciona a busca de cidades no Provider

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 4: Gateway — previsão e busca de cidades

**Files:**
- Create: `Gateway/Climato.Gateway.Application/Model/Weathers/ForecastDto.cs`, `Model/Weathers/ForecastDayDto.cs`, `Model/Locations/LocationDto.cs`, `Model/Locations/LocationSearchQuery.cs`, `Validators/LocationSearchQueryValidator.cs`, `Contracts/ILocationAppService.cs`, `Services/LocationAppService.cs`, `Gateway/Climato.Gateway.Api/Controllers/ForecastController.cs`, `Controllers/LocationController.cs`
- Modify: `Contracts/IWeatherProviderClient.cs`, `Contracts/IWeatherAppService.cs`, `Services/WeatherAppService.cs`, `Gateway/Climato.Gateway.Application/DependencyInjection.cs`, `Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs`

**Interfaces:**
- Consumes: `GET /forecasts?cityName=` e `GET /locations?query=` do Provider (Tarefas 2 e 3).
- Produces: `IWeatherProviderClient.GetForecastAsync(string, CancellationToken) : Task<ForecastDto?>` e `SearchLocationsAsync(string, CancellationToken) : Task<IReadOnlyList<LocationDto>>`; `IWeatherAppService.GetForecastAsync(string, CancellationToken) : Task<ForecastDto?>`; `ILocationAppService.SearchAsync(string, CancellationToken) : Task<IReadOnlyList<LocationDto>>`; `GET /forecasts/{cityName}` e `GET /locations?query=` no Gateway.

- [x] **Passo 1: `Model/Weathers/ForecastDayDto.cs` (criar)**

```csharp
namespace Climato.Gateway.Application.Model.Weathers;

public sealed class ForecastDayDto
{
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required double TempMin { get; init; }
    public required double TempMax { get; init; }
    public required int PrecipitationProbability { get; init; }
    public required double WindSpeedKmh { get; init; }
}
```

- [x] **Passo 2: `Model/Weathers/ForecastDto.cs` (criar)**

```csharp
namespace Climato.Gateway.Application.Model.Weathers;

public sealed class ForecastDto
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required IReadOnlyList<ForecastDayDto> Days { get; init; }
}
```

- [x] **Passo 3: `Model/Locations/LocationDto.cs` (criar)**

```csharp
namespace Climato.Gateway.Application.Model.Locations;

public sealed class LocationDto
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public required string Country { get; init; }
}
```

- [x] **Passo 4: `Model/Locations/LocationSearchQuery.cs` (criar)** — `string?` para não cair no `[Required]` implícito (ver Ajustes).

```csharp
namespace Climato.Gateway.Application.Model.Locations;

public sealed class LocationSearchQuery
{
    public string? Query { get; init; }
}
```

- [x] **Passo 5: `Validators/LocationSearchQueryValidator.cs` (criar)**

```csharp
using Climato.Gateway.Application.Model.Locations;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class LocationSearchQueryValidator : AbstractValidator<LocationSearchQuery>
{
    public LocationSearchQueryValidator()
    {
        RuleFor(x => x.Query)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Parâmetro 'query' deve ter entre 2 e 100 caracteres.")
            .Must(query => query!.Trim().Length is >= 2 and <= 100).WithMessage("Parâmetro 'query' deve ter entre 2 e 100 caracteres.");
    }
}
```

- [x] **Passo 6: `Contracts/IWeatherProviderClient.cs`**

```csharp
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherProviderClient
{
    Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken);
    Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(string query, CancellationToken cancellationToken);
}
```

- [x] **Passo 7: `Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs` (reescrever)** — o tratamento de erro atual vai para `GetAsync<T>`.

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Exceptions;
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Model.Weathers;
using Polly;

namespace Climato.Gateway.Infrastructure.Http;

public class WeatherProviderClient : IWeatherProviderClient
{
    private readonly HttpClient client;

    public WeatherProviderClient(HttpClient httpClient)
    {
        client = httpClient;
    }

    public Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        return GetAsync<WeatherDto>($"weathers?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);
    }

    public Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        return GetAsync<ForecastDto>($"forecasts?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);
    }

    public async Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(string query, CancellationToken cancellationToken)
    {
        return await GetAsync<List<LocationDto>>($"locations?query={Uri.EscapeDataString(query)}", cancellationToken) ?? [];
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderUnavailableException($"O Provider respondeu {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new ProviderUnavailableException("O Provider respondeu sem corpo.");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ProviderUnavailableException("Falha ao chamar o Provider.", exception);
        }
    }
}
```

- [x] **Passo 8: `Contracts/IWeatherAppService.cs` e `Services/WeatherAppService.cs`** — acrescentar na interface:

```csharp
    Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken);
```

e no serviço (sem gravar histórico):

```csharp
    public Task<ForecastDto?> GetForecastAsync(string cityName, CancellationToken cancellationToken)
    {
        return weatherProviderClient.GetForecastAsync(cityName, cancellationToken);
    }
```

- [x] **Passo 9: `Contracts/ILocationAppService.cs` e `Services/LocationAppService.cs` (criar)**

```csharp
using Climato.Gateway.Application.Model.Locations;

namespace Climato.Gateway.Application.Contracts;

public interface ILocationAppService
{
    Task<IReadOnlyList<LocationDto>> SearchAsync(string query, CancellationToken cancellationToken);
}
```

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Locations;

namespace Climato.Gateway.Application.Services;

public class LocationAppService : ILocationAppService
{
    private readonly IWeatherProviderClient weatherProviderClient;

    public LocationAppService(IWeatherProviderClient providerClient)
    {
        weatherProviderClient = providerClient;
    }

    public Task<IReadOnlyList<LocationDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        return weatherProviderClient.SearchLocationsAsync(query.Trim(), cancellationToken);
    }
}
```

- [x] **Passo 10: `Gateway/Climato.Gateway.Application/DependencyInjection.cs`** — após `IWeatherAppService`:

```csharp
        services.AddScoped<ILocationAppService, LocationAppService>();
```

- [x] **Passo 11: `Gateway/Climato.Gateway.Api/Controllers/ForecastController.cs` (criar)**

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("forecasts")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public ForecastController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna a previsão de até 5 dias da cidade informada, agregada por dia no fuso da cidade. Não registra a cidade no histórico.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Previsão diária: mínima, máxima, condição do meio-dia, chance de chuva (%) e vento máximo em km/h.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet("{cityName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ForecastDto>> GetForecast(string cityName, CancellationToken cancellationToken)
    {
        var forecast = await appService.GetForecastAsync(cityName, cancellationToken);

        if (forecast == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(forecast);
    }
}
```

- [x] **Passo 12: `Gateway/Climato.Gateway.Api/Controllers/LocationController.cs` (criar)** — `request.Query!`: o `ValidationFilter` já barrou nulo/vazio; o parâmetro não pode se chamar `query` (ver Ajustes).

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("locations")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationAppService appService;

    public LocationController(ILocationAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Busca cidades pelo nome para o autocomplete (até 5 resultados).</summary>
    /// <param name="request">Parte do nome da cidade em `query` (2 a 100 caracteres), exemplo: São Pa</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidades encontradas, com nome em português quando houver; lista vazia sem resultado.</response>
    /// <response code="400">Parâmetro 'query' com menos de 2 ou mais de 100 caracteres.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> Search([FromQuery] LocationSearchQuery request, CancellationToken cancellationToken)
    {
        var locations = await appService.SearchAsync(request.Query!, cancellationToken);
        return Ok(locations);
    }
}
```

- [x] **Passo 13: Build** — `dotnet build Climato.slnx` → sem erros e sem avisos `CS86xx`.

- [x] **Passo 14: Commit** (após `/revisar-padrao`)

```bash
git add src/Gateway
git commit -m "Expõe a previsão e a busca de cidades no Gateway

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 5: Documentação de arquitetura e checklist

**Files:**
- Modify: `docs/architecture/backend.md`, `docs/architecture/system-design.md`, `docs/architecture/review-checklist.md`, `docs/architecture/frontend.md`

- [x] **Passo 1: `backend.md`**
  - **Estrutura da Solução:** linha do `Climato.Provider.Domain` → `← Weathers (Weather, Forecast — contratos devolvidos ao Gateway), Locations`; linha do `Climato.Provider.Application` → `← OpenWeatherClient, AppServices (clima, previsão, busca, imagem), cache (Services), Contracts, Model (DTOs da OpenWeather/Pexels), Exceptions, Options, DependencyInjection.cs`.
  - **Controllers:** exemplo de rotas → `(/cities, /weathers, /forecasts, /locations, /auth)`.
  - **Comunicação HTTP:** (feito na Tarefa 1) novo item — "Provider: toda chamada à OpenWeather passa pelo typed client `IOpenWeatherClient` (`OpenWeatherClient`), que concentra a tradução de erros num `GetAsync<T>` privado; AppServices não recebem `HttpClient` da OpenWeather. No Gateway, o `WeatherProviderClient` faz o mesmo para o Provider."
  - **Cache (Provider):** (feito na Tarefa 1) trocar o primeiro item por "`IDistributedCache` atrás de `IProviderCache` (`Services/ProviderCache`), genérico: `GetAsync<T>(prefixo, texto)` / `SetAsync<T>(prefixo, texto, valor, duração)`. Redis se `ConnectionStrings:Redis` estiver preenchida; senão `AddDistributedMemoryCache`." e o segundo por "Chave `{prefixo}:{texto-normalizado}` (trim, espaços colapsados, minúsculas, sem acentos); valor em JSON; TTL absoluto por uso: `clima` 10 min, `previsao` 30 min, `locais` 24 h (lista vazia também é cacheada)."
  - **Decisões Técnicas — Provider como camada anticorrupção:** acrescentar "A previsão é agregada por dia no Provider (data local da cidade, mín/máx, condição do ponto mais próximo de 12:00, maior `pop` × 100, maior vento em km/h); o Gateway e o front não veem os pontos de 3 h."

- [x] **Passo 2: `system-design.md`**
  - Após **Consulta de clima**, novas subseções:

```markdown
### Previsão de 5 dias

1. `GET /forecasts/{cidade}` no Gateway com `Authorization: Bearer`.
2. Gateway chama `GET /forecasts?cityName=` no Provider.
3. Provider consulta o cache (`previsao:{cidade-normalizada}`, TTL 30 min); em miss, chama OpenWeather `data/2.5/forecast` e agrega os pontos de 3 h por dia no fuso da cidade (até 5 dias).
4. Gateway devolve a previsão **sem** registrar a cidade no histórico.

### Busca de cidades (autocomplete)

1. `GET /locations?query=` no Gateway (2 a 100 caracteres; senão 400).
2. Gateway chama `GET /locations?query=` no Provider.
3. Provider consulta o cache (`locais:{texto-normalizado}`, TTL 24 h); em miss, chama OpenWeather `geo/1.0/direct` (`limit=5`) e devolve `{ name, state, country }`, com o nome em português quando houver.
```

  - **Mapeamento de erros:** acrescentar as linhas

```markdown
| Busca com `query` < 2 ou > 100 caracteres | 400 | 400 |
| Busca sem resultado | 200 `[]` | 200 `[]` |
```

  e trocar "Cidade não existe na OpenWeather" por "Cidade não existe na OpenWeather (clima ou previsão)".
  - **Integrações externas:** remover "(Fase 03)" das linhas da previsão e do geocoding.

- [x] **Passo 3: `review-checklist.md`** (feito na Tarefa 1) — nova linha após BE-27:

```markdown
| BE-28 | 🟡 | 03 | Chamada à OpenWeather fora do client | `HttpClient` da OpenWeather injetado em AppService ou URL `data/2.5`/`geo/1.0` montada fora do `OpenWeatherClient` | [Comunicação HTTP](backend.md#comunicação-http) |
```

- [x] **Passo 4: `frontend.md`** — em **Chamadas à API**, novo item: "Previsão (`GET /forecasts/{cidade}`) e busca de cidades (`GET /locations?query=`, mínimo 2 caracteres) já existem no Gateway; o consumo (autocomplete com *debounce*, tela de previsão) é da Fase 07."

- [x] **Passo 5: Commit** (após `/revisar-padrao`)

```bash
git add docs/architecture
git commit -m "Documenta previsão, busca de cidades, OpenWeatherClient e cache genérico

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 6: `project-overview` — conclusão da Fase 03

**Files:**
- Modify: `docs/project-overview.md`

- [x] **Passo 1: Fase 03** — trocar o bloco por:

```markdown
#### Fase 03 — Comunicação entre serviços ✅ Concluída (AAAA-MM-DD)
Dividida em dois blocos:
- **03a — Contrato e robustez** ✅ (2026-10-19): DTOs tipados, erros 404/503, resiliência, cache (Redis opcional), health checks, vento em km/h e horários no fuso da cidade. [Design](features/03-comunicacao-servicos/design.md) · [Plano](features/03-comunicacao-servicos/plan.md)
- **03b — Novos endpoints** ✅ (AAAA-MM-DD): previsão de 5 dias e busca de cidades (geocoding), com `OpenWeatherClient` único e cache genérico no Provider. [Design](features/03-comunicacao-servicos/endpoints-design.md) · [Plano](features/03-comunicacao-servicos/endpoints-plan.md)

Auditoria: A14–A17, A19 (resolvidas na 03a).
```

- [x] **Passo 2: Estado atual** — título `## Estado Atual (AAAA-MM-DD, após a Fase 03)`; em **Funciona**, acrescentar "previsão de 5 dias e busca de cidades na API (ainda sem tela no front)".

- [x] **Passo 3: Pendências** — acrescentar:

```markdown
- [Fase 07] Autocomplete da busca com *debounce* (~300 ms) e mínimo de 2 caracteres antes de chamar `GET /locations`
- [Fase 04] Previsão: resposta 200 da OpenWeather com `list` vazia ou `weather` vazio em algum ponto vira 500 — mesma correção da resposta incompleta do clima atual
```

- [x] **Passo 4: Commit** (após `/revisar-padrao`; o `AAAA-MM-DD` é trocado pela data real no commit final da Tarefa 7)

```bash
git add docs/project-overview.md
git commit -m "Registra a conclusão da Fase 03 no project-overview

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 7: Verificação final e PR

- [x] **Passo 1:** `/verification-before-completion` com os comandos da [Verificação](../../../CLAUDE.md#verificação):

```bash
dotnet build Climato.slnx --no-incremental
npm --prefix web run lint
npm --prefix web run build
```

Esperado: build sem erros e sem avisos `CS86xx`; lint limpo; build do web ok (front não mudou).

- [x] **Passo 2:** roteiro manual da seção [Verificação do design](endpoints-design.md#verificação) (itens 1–7) pelo Scalar, anotando o resultado de cada um no PR. Para o item 2, conferir no log do `System.Net.Http.HttpClient` do Provider que a segunda chamada não sai para a OpenWeather.

- [x] **Passo 3:** `/revisar-padrao` no diff completo da branch → sem 🔴 (ou liberados pelo usuário).

- [ ] **Passo 4:** `/requesting-code-review`.

- [ ] **Passo 5:** `/finishing-a-development-branch` → PR `dev/novos-endpoints` → `master` "Fase 03b: previsão de 5 dias e busca de cidades"; trocar os `AAAA-MM-DD` da Tarefa 6 pela data real no commit final.
