# Fase 03a — Contrato e robustez: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** trocar o `JsonDictionary` por contratos tipados, mapear cada falha para o status certo, adicionar resiliência, cache e health checks e corrigir vento e fuso, conforme o [design aprovado](design.md).

**Arquitetura:** primeiro o Provider (contrato, erros e resiliência; depois cache e health), depois o Gateway (contrato, erros, resiliência e health; depois as pendências de repositório, construtores, 400 automático e nulabilidade), depois o ajuste mínimo do front e a documentação. Cada tarefa compila sozinha e vira um commit.

**Stack:** .NET 10 · System.Text.Json (`System.Net.Http.Json`) · Microsoft.Extensions.Http.Resilience 10.11.0 · Microsoft.Extensions.Caching.StackExchangeRedis 10.0.13 · Microsoft.Extensions.Caching.Memory 10.0.13 · AspNetCore.HealthChecks.Redis 9.0.0 · Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.13 · React 19.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) e [`frontend.md`](../../architecture/frontend.md) antes de editar. O design vale mais que este plano em caso de conflito.
- **Sem projeto de testes** (Fase 04). Cada tarefa termina com `dotnet build Climato.slnx` sem erros; tarefas do front com `npm --prefix web run lint` e `npm --prefix web run build`. Verificação funcional na Tarefa 8.
- **Commits:** um por tarefa (0–7), com as mensagens indicadas. A aprovação deste plano é a autorização explícita para esses commits (mesma exceção das Fases 01 e 02 à regra "nunca commitar durante a fase"). Antes de cada commit, `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/contrato-robustez`, a partir do `origin/master`.
- **Nunca** gravar chaves (OpenWeather, Pexels, `Jwt:SecretKey`) nem connection string com senha em arquivo versionado.
- Fora do escopo: previsão de 5 dias e geocoding (03b); testes (04); Redis no compose (05); Serilog (06); `CityCard` (07).
- Código C#: chaves em todo `if`/`else`/`foreach`; campos privados `camelCase` sem `_`; namespace file-scoped; mensagens em português; `CancellationToken` sem default nas interfaces (exceto onde a interface do framework define default, como `IHealthCheck`); sem comentários do "o quê".
- Versões só em `Directory.Packages.props`; `.csproj` sem `Version`. Conferir o `.csproj` antes de usar namespace de pacote externo.

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| `using` em `HttpRequestMessage` do Pexels | O header `Authorization` do Pexels vai para `DefaultRequestHeaders` no registro do client; o serviço usa `GetAsync` e não cria `HttpRequestMessage` | Remove a mensagem manual (e o `using` que faltava); a chave sai do serviço |
| `GetImageUrlAsync(string? cityName)` | `GetImageUrlAsync(string cityName)` | O nome agora vem do contrato tipado (`required`); a checagem de vazio virou impossível (GE-04) |
| Pacotes da seção 7 | + `Microsoft.Extensions.Caching.Memory` no `Provider.Application` | `AddDistributedMemoryCache` mora nesse pacote; não vem garantido pelos outros |
| `AspNetCore.HealthChecks.Redis` sem versão | `9.0.0` | Última versão publicada; compatível com .NET 10 |
| Health checks do Provider no `Program.cs` | Novo `Climato.Provider.Api/DependencyInjection.cs` com `AddProviderApi(configuration)` | Mesmo padrão do Gateway (`backend.md` — `Program.cs` só chama extensões) |
| `WeatherDetails`: erro ≠ 404 → `message.error` | Erro ≠ 404 → estado de erro com a mensagem da API no mesmo `Empty` da tela | Hoje `weather` fica `null` e a tela quebra em `weather.city`; um estado explícito resolve as duas coisas (FE-10) |
| Redis fora → warning e segue | Idem, e o runbook recomenda `abortConnect=false,connectTimeout=1000` na connection string | Sem isso, cada requisição espera o timeout padrão de 5 s do StackExchange.Redis |
| Circuit breaker (janela ≥ 2× tentativa) | Gateway → Provider com `SamplingDuration` = 60 s; Provider usa o padrão (30 s) | 30 s < 2 × 25 s falharia a validação do pacote no startup |
| Gateway → Provider 20 s/25 s, Pexels 3 s/5 s, retry padrão, Redis `Unhealthy` (I1–I3) | Gateway → Provider 25 s/30 s; Pexels 2 s/5 s; Gateway re-tenta só `HttpRequestException`; Redis com `failureStatus: Degraded` | Decidido na revisão final: a tentativa do Gateway precisa superar OpenWeather 15 s + Pexels 5 s + cache; 5xx do Provider já passou por retries; Redis fora não deve derrubar o `ready` do Gateway |

## Mapa de arquivos

Caminhos relativos a `src/`.

| Arquivo | Ação | Tarefa |
|---|---|---|
| `../Directory.Packages.props` | Modificar | 1, 2, 3, 4 |
| `Provider/Climato.Provider.Domain/Weathers/Weather.cs` | Reescrever | 1 |
| `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherCurrentResponse.cs` | Criar | 1 |
| `Provider/Climato.Provider.Application/Model/Pexels/PexelsSearchResponse.cs` | Criar | 1 |
| `Provider/Climato.Provider.Application/Exceptions/ExternalServiceUnavailableException.cs` | Criar | 1 |
| `Provider/Climato.Provider.Application/Contracts/{IWeatherAppService,ICityImageAppService}.cs` | Modificar | 1 |
| `Provider/Climato.Provider.Application/Services/{WeatherAppService,CityImageAppService}.cs` | Reescrever | 1, 2 |
| `Provider/Climato.Provider.Application/Core/Collections/*` | Remover | 1 |
| `Provider/Climato.Provider.Application/DependencyInjection.cs`, `.csproj` | Modificar | 1, 2 |
| `Provider/Climato.Provider.Application/Contracts/IWeatherCache.cs`, `Services/WeatherCache.cs` | Criar | 2 |
| `Provider/Climato.Provider.Api/DependencyInjection.cs` | Criar | 1, 2 |
| `Provider/Climato.Provider.Api/Errors/GlobalExceptionHandler.cs` | Criar | 1 |
| `Provider/Climato.Provider.Api/Controllers/WeatherController.cs` | Reescrever | 1 |
| `Provider/Climato.Provider.Api/Program.cs`, `appsettings.json`, `.csproj` | Modificar | 1, 2 |
| `Gateway/Climato.Gateway.Application/Model/Weathers/WeatherDto.cs` | Reescrever | 3 |
| `Gateway/Climato.Gateway.Application/Exceptions/ProviderUnavailableException.cs` | Criar | 3 |
| `Gateway/Climato.Gateway.Application/Contracts/{IWeatherAppService,IWeatherProviderClient}.cs` | Modificar | 3 |
| `Gateway/Climato.Gateway.Application/Services/WeatherAppService.cs` | Reescrever | 3 |
| `Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs` | Reescrever | 3 |
| `Gateway/Climato.Gateway.Infrastructure/Http/ProviderHealthCheck.cs` | Criar | 3 |
| `Gateway/Climato.Gateway.Infrastructure/Http/{JsonDictionary,HttpResponseMessageExtensions}.cs` | Remover | 3 |
| `Gateway/Climato.Gateway.Infrastructure/DependencyInjection.cs`, `.csproj` | Modificar | 3 |
| `Gateway/Climato.Gateway.Api/Errors/GlobalExceptionHandler.cs` | Reescrever | 3, 4 |
| `Gateway/Climato.Gateway.Api/Controllers/WeatherController.cs` | Reescrever | 3 |
| `Gateway/Climato.Gateway.Api/Program.cs` | Modificar | 3 |
| `Gateway/Climato.Gateway.Domain/Exceptions/UserNotFoundException.cs` | Criar | 4 |
| `Gateway/Climato.Gateway.Infrastructure/Data/Repositories/{UserRepository,CityRepository}.cs` | Modificar | 4 |
| `Gateway/Climato.Gateway.Application/Services/CityAppService.cs` | Modificar | 4 |
| `Gateway/Climato.Gateway.Api/DependencyInjection.cs` | Modificar | 4 |
| `../.editorconfig` | Modificar | 4 |
| `../web/src/pages/WeatherDetails.jsx`, `../web/src/components/formatDatetime.jsx` | Modificar | 5 |
| `../docs/**` | Modificar | 6, 7 |

---

### Tarefa 0: Preparar a branch

- [ ] **Passo 1:** `/using-git-worktrees` → branch `dev/contrato-robustez` a partir do `origin/master` (`git fetch` antes; `git log --oneline -1 origin/master` deve mostrar `Merge pull request #9 from YudiKojima/dev/branch-master` ou posterior).
- [ ] **Passo 2:** copiar `docs/features/03-comunicacao-servicos/design.md` e `plan.md` para a branch e commitar:

```bash
git add docs/features/03-comunicacao-servicos
git commit -m "Adiciona design e plano da Fase 03a (contrato e robustez)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Passo 3:** conferir que os user-secrets do Provider (`OpenWeather:ApiKey`, `Pexels:ApiKey`) e do Gateway (`ConnectionStrings:Climato`, `Jwt:SecretKey`) existem nesta máquina (`dotnet user-secrets list --project ...` — **sem** exibir os valores no chat). Os worktrees compartilham os user-secrets pelo `UserSecretsId`.

---

### Tarefa 1: Provider — contrato tipado, erros e resiliência (A14, A16, A17, A19)

**Files:**
- Create: `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherCurrentResponse.cs`, `Model/Pexels/PexelsSearchResponse.cs`, `Exceptions/ExternalServiceUnavailableException.cs`, `Provider/Climato.Provider.Api/DependencyInjection.cs`, `Provider/Climato.Provider.Api/Errors/GlobalExceptionHandler.cs`
- Modify: `Directory.Packages.props`, `Provider/Climato.Provider.Domain/Weathers/Weather.cs`, `Contracts/IWeatherAppService.cs`, `Contracts/ICityImageAppService.cs`, `Services/WeatherAppService.cs`, `Services/CityImageAppService.cs`, `Provider/Climato.Provider.Application/DependencyInjection.cs`, `Climato.Provider.Application.csproj`, `Provider/Climato.Provider.Api/Controllers/WeatherController.cs`, `Program.cs`
- Delete: `Provider/Climato.Provider.Application/Core/Collections/JsonDictionary.cs`, `Core/Collections/HttpResponseMessageExtensions.cs`

**Interfaces:**
- Produces: `Weather` com `WindSpeedKmh` e `Sunrise`/`Sunset` `DateTimeOffset`; `IWeatherAppService.GetByCityAsync(string, CancellationToken) : Task<Weather?>`; `ICityImageAppService.GetImageUrlAsync(string, CancellationToken) : Task<string?>`; `ExternalServiceUnavailableException(string serviceName, string reason, Exception? innerException = null)`; `AddProviderApplication(IConfiguration)`; `AddProviderApi(IConfiguration)`.

- [ ] **Passo 1: Pacote** — em `Directory.Packages.props`, em ordem alfabética (após `Microsoft.Extensions.Http`):

```xml
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.11.0" />
```

Em `Climato.Provider.Application.csproj`, trocar `<PackageReference Include="Newtonsoft.Json" />` por:

```xml
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
```

(O `Newtonsoft.Json` continua no `Directory.Packages.props` até a Tarefa 3, porque o Gateway ainda usa.)

- [ ] **Passo 2: `Provider/Climato.Provider.Domain/Weathers/Weather.cs` (reescrever)**

```csharp
namespace Climato.Provider.Domain.Weathers;

public sealed class Weather
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required double Temperature { get; init; }
    public required double FeelsLike { get; init; }
    public required double TempMin { get; init; }
    public required double TempMax { get; init; }
    public required int Humidity { get; init; }
    public required int Pressure { get; init; }
    public required double WindSpeedKmh { get; init; }
    public required int WindDirection { get; init; }
    public required DateTimeOffset Sunrise { get; init; }
    public required DateTimeOffset Sunset { get; init; }
    public string? ImageUrl { get; init; }
}
```

- [ ] **Passo 3: `Provider/Climato.Provider.Application/Model/OpenWeather/OpenWeatherCurrentResponse.cs`**

```csharp
namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherCurrentResponse(
    string Name,
    int Timezone,
    OpenWeatherSys Sys,
    IReadOnlyList<OpenWeatherCondition> Weather,
    OpenWeatherMain Main,
    OpenWeatherWind Wind);

public sealed record OpenWeatherSys(string Country, long Sunrise, long Sunset);

public sealed record OpenWeatherCondition(string Description, string Icon);

public sealed record OpenWeatherMain(double Temp, double FeelsLike, double TempMin, double TempMax, int Humidity, int Pressure);

public sealed record OpenWeatherWind(double Speed, int Deg);
```

- [ ] **Passo 4: `Provider/Climato.Provider.Application/Model/Pexels/PexelsSearchResponse.cs`**

```csharp
namespace Climato.Provider.Application.Model.Pexels;

public sealed record PexelsSearchResponse(IReadOnlyList<PexelsPhoto> Photos);

public sealed record PexelsPhoto(PexelsPhotoSource Src);

public sealed record PexelsPhotoSource(string Large2x);
```

- [ ] **Passo 5: `Provider/Climato.Provider.Application/Exceptions/ExternalServiceUnavailableException.cs`**

```csharp
namespace Climato.Provider.Application.Exceptions;

public sealed class ExternalServiceUnavailableException : Exception
{
    public ExternalServiceUnavailableException(string serviceName, string reason, Exception? innerException = null)
        : base($"{serviceName} indisponível: {reason}.", innerException)
    {
    }
}
```

- [ ] **Passo 6: Contratos** — `Contracts/IWeatherAppService.cs`:

```csharp
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts;

public interface IWeatherAppService
{
    Task<Weather?> GetByCityAsync(string cityName, CancellationToken cancellationToken);
}
```

`Contracts/ICityImageAppService.cs`:

```csharp
namespace Climato.Provider.Application.Contracts;

public interface ICityImageAppService
{
    Task<string?> GetImageUrlAsync(string cityName, CancellationToken cancellationToken);
}
```

- [ ] **Passo 7: `Services/CityImageAppService.cs` (reescrever)**

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Model.Pexels;
using Microsoft.Extensions.Logging;
using Polly;

namespace Climato.Provider.Application.Services;

public sealed class CityImageAppService : ICityImageAppService
{
    private readonly HttpClient client;
    private readonly ILogger<CityImageAppService> logger;

    public CityImageAppService(HttpClient httpClient, ILogger<CityImageAppService> logger)
    {
        client = httpClient;
        this.logger = logger;
    }

    public async Task<string?> GetImageUrlAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"v1/search?query={Uri.EscapeDataString(cityName)}&orientation=landscape&per_page=1";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Pexels respondeu {StatusCode} para {CityName}; clima segue sem imagem.", (int)response.StatusCode, cityName);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PexelsSearchResponse>(cancellationToken);
            return result?.Photos.FirstOrDefault()?.Src.Large2x;
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            logger.LogWarning(exception, "Falha ao buscar a imagem de {CityName} no Pexels; clima segue sem imagem.", cityName);
            return null;
        }
    }
}
```

- [ ] **Passo 8: `Services/WeatherAppService.cs` (reescrever)**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Exceptions;
using Climato.Provider.Application.Model.OpenWeather;
using Climato.Provider.Application.Options;
using Climato.Provider.Domain.Weathers;
using Microsoft.Extensions.Options;
using Polly;

namespace Climato.Provider.Application.Services;

public sealed class WeatherAppService : IWeatherAppService
{
    private const string ServiceName = "OpenWeather";

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly ICityImageAppService cityImageAppService;

    public WeatherAppService(HttpClient httpClient, IOptions<OpenWeatherOptions> options, ICityImageAppService cityImageAppService)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
        this.cityImageAppService = cityImageAppService;
    }

    public async Task<Weather?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var current = await GetCurrentAsync(cityName, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = await cityImageAppService.GetImageUrlAsync(current.Name, cancellationToken);
        return ToWeather(current, imageUrl);
    }

    private async Task<OpenWeatherCurrentResponse?> GetCurrentAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";

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

            return await response.Content.ReadFromJsonAsync<OpenWeatherCurrentResponse>(jsonOptions, cancellationToken)
                ?? throw new ExternalServiceUnavailableException(ServiceName, "resposta vazia");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ExternalServiceUnavailableException(ServiceName, "falha na chamada", exception);
        }
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

- [ ] **Passo 9: remover** `Provider/Climato.Provider.Application/Core/Collections/JsonDictionary.cs` e `Core/Collections/HttpResponseMessageExtensions.cs` (a pasta `Core/` fica vazia e sai junto).

- [ ] **Passo 10: `Provider/Climato.Provider.Application/DependencyInjection.cs` (reescrever)**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Options;
using Climato.Provider.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenWeatherOptions>()
            .BindConfiguration(OpenWeatherOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PexelsOptions>()
            .BindConfiguration(PexelsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IWeatherAppService, WeatherAppService>((serviceProvider, client) =>
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

        services.AddHttpClient<ICityImageAppService, CityImageAppService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PexelsOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", options.ApiKey);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.Retry.MaxRetryAttempts = 1;
            });

        return services;
    }
}
```

(`configuration` passa a ser usado na Tarefa 2, no cache; já entra na assinatura agora para o `Program.cs` não mudar duas vezes.)

- [ ] **Passo 11: `Provider/Climato.Provider.Api/Errors/GlobalExceptionHandler.cs`**

```csharp
using Climato.Provider.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Requisição cancelada pelo cliente em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        ProblemDetails problem;

        if (exception is ExternalServiceUnavailableException)
        {
            logger.LogWarning(exception, "Serviço externo indisponível em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Serviço de clima indisponível."
            };
        }
        else
        {
            logger.LogError(exception, "Erro não tratado em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno."
            };
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
```

- [ ] **Passo 12: `Provider/Climato.Provider.Api/DependencyInjection.cs`**

```csharp
using Climato.Provider.Api.Errors;

namespace Climato.Provider.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();

        return services;
    }
}
```

(`configuration` passa a ser usado na Tarefa 2, no health check do Redis.)

- [ ] **Passo 13: `Provider/Climato.Provider.Api/Controllers/WeatherController.cs` (reescrever)**

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Weathers;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.Api.Controllers;

[ApiController]
[Route("weathers")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna o clima atual e a imagem da cidade informada.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Clima atual, com vento em km/h e horários no fuso da cidade.</response>
    /// <response code="400">Parâmetro 'cityName' ausente.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">OpenWeather indisponível.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Weather>> Get([FromQuery] string cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Parâmetro 'cityName' é obrigatório.");
        }

        var weather = await appService.GetByCityAsync(cityName, cancellationToken);

        if (weather == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(weather);
    }
}
```

- [ ] **Passo 14: `Provider/Climato.Provider.Api/Program.cs` (reescrever)**

```csharp
using Climato.Provider.Api;
using Climato.Provider.Application;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddProviderApi(builder.Configuration)
    .AddProviderApplication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
```

(Sai o `UseAuthorization()`: o Provider não tem autenticação nem `[Authorize]`.)

- [ ] **Passo 15: Build**

```bash
dotnet build Climato.slnx
```

Esperado: `Build succeeded`, 0 erros.

- [ ] **Passo 16: Smoke test** — subir o Provider (`dotnet run --project src/Provider/Climato.Provider.Api --launch-profile https`) e, no Scalar (`https://localhost:7002/scalar`):
  - `GET /weathers?cityName=Tóquio` → 200, `windSpeedKmh` presente, `sunrise` terminando em `+09:00`.
  - `GET /weathers?cityName=cidadeinexistentexyz` → 404 `application/problem+json`, título "Cidade não encontrada.".
  - `GET /health/live` e `GET /health/ready` → 200 `Healthy`.

  Parar o serviço.

- [ ] **Passo 17: Commit** (após `/revisar-padrao`)

```bash
git add Directory.Packages.props src/Provider
git commit -m "Tipa o contrato do Provider e mapeia falhas externas para 503

Resolve A14, A16, A17 e A19 no Provider: DTOs da OpenWeather e do Pexels com
System.Text.Json, vento em km/h, nascer/pôr do sol no fuso da cidade, Pexels
opcional e resiliência nos clients HTTP.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 2: Provider — cache e health check do Redis

**Files:**
- Create: `Provider/Climato.Provider.Application/Contracts/IWeatherCache.cs`, `Services/WeatherCache.cs`
- Modify: `Directory.Packages.props`, `Climato.Provider.Application.csproj`, `Climato.Provider.Api.csproj`, `Provider/Climato.Provider.Application/DependencyInjection.cs`, `Services/WeatherAppService.cs`, `Provider/Climato.Provider.Api/DependencyInjection.cs`, `appsettings.json`

**Interfaces:**
- Consumes: `Weather`, `AddProviderApplication(IConfiguration)`, `AddProviderApi(IConfiguration)` (Tarefa 1).
- Produces: `IWeatherCache.GetAsync(string cityName, CancellationToken) : Task<Weather?>`; `IWeatherCache.SetAsync(string cityName, Weather weather, CancellationToken) : Task`; configuração `ConnectionStrings:Redis`.

- [ ] **Passo 1: Pacotes** — em `Directory.Packages.props`, em ordem alfabética:

```xml
    <PackageVersion Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.13" />
```

(`AspNetCore.HealthChecks.Redis` antes de `FluentValidation`; os dois `Caching` antes de `Microsoft.Extensions.Http`.)

`Climato.Provider.Application.csproj`, no `ItemGroup` de pacotes, em ordem alfabética:

```xml
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
```

`Climato.Provider.Api.csproj`, no `ItemGroup` de pacotes, primeiro item:

```xml
    <PackageReference Include="AspNetCore.HealthChecks.Redis" />
```

- [ ] **Passo 2: `Contracts/IWeatherCache.cs`**

```csharp
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts;

public interface IWeatherCache
{
    Task<Weather?> GetAsync(string cityName, CancellationToken cancellationToken);
    Task SetAsync(string cityName, Weather weather, CancellationToken cancellationToken);
}
```

- [ ] **Passo 3: `Services/WeatherCache.cs`**

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using Climato.Provider.Application.Contracts;
using Climato.Provider.Domain.Weathers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Climato.Provider.Application.Services;

public sealed class WeatherCache : IWeatherCache
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly DistributedCacheEntryOptions entryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly IDistributedCache cache;
    private readonly ILogger<WeatherCache> logger;

    public WeatherCache(IDistributedCache cache, ILogger<WeatherCache> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<Weather?> GetAsync(string cityName, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await cache.GetAsync(CreateKey(cityName), cancellationToken);
            return bytes == null ? null : JsonSerializer.Deserialize<Weather>(bytes, jsonOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao ler o cache de clima de {CityName}; seguindo sem cache.", cityName);
            return null;
        }
    }

    public async Task SetAsync(string cityName, Weather weather, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(weather, jsonOptions);
            await cache.SetAsync(CreateKey(cityName), bytes, entryOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Falha ao gravar o cache de clima de {CityName}; seguindo sem cache.", cityName);
        }
    }

    private static string CreateKey(string cityName)
    {
        var words = cityName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var decomposed = string.Join(' ', words).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return $"clima:{builder.ToString().Normalize(NormalizationForm.FormC)}";
    }
}
```

- [ ] **Passo 4: `Services/WeatherAppService.cs`** — injetar o cache e usá-lo em `GetByCityAsync`:
  - Adicionar o campo `private readonly IWeatherCache weatherCache;`.
  - Construtor passa a ser:

```csharp
    public WeatherAppService(HttpClient httpClient, IOptions<OpenWeatherOptions> options, ICityImageAppService cityImageAppService, IWeatherCache weatherCache)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
        this.cityImageAppService = cityImageAppService;
        this.weatherCache = weatherCache;
    }
```

  - `GetByCityAsync` passa a ser:

```csharp
    public async Task<Weather?> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var cached = await weatherCache.GetAsync(cityName, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var current = await GetCurrentAsync(cityName, cancellationToken);

        if (current == null)
        {
            return null;
        }

        var imageUrl = await cityImageAppService.GetImageUrlAsync(current.Name, cancellationToken);
        var weather = ToWeather(current, imageUrl);

        await weatherCache.SetAsync(cityName, weather, cancellationToken);
        return weather;
    }
```

- [ ] **Passo 5: `Provider/Climato.Provider.Application/DependencyInjection.cs`** — logo após os dois `AddOptions`, antes dos `AddHttpClient`:

```csharp
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
        }

        services.AddSingleton<IWeatherCache, WeatherCache>();
```

- [ ] **Passo 6: `Provider/Climato.Provider.Api/DependencyInjection.cs`** — trocar `services.AddHealthChecks();` por:

```csharp
        var healthChecks = services.AddHealthChecks();
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecks.AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);
        }
```

- [ ] **Passo 7: `Provider/Climato.Provider.Api/appsettings.json`** — adicionar após `"AllowedHosts": "*",`:

```json
  "ConnectionStrings": {
    "Redis": ""
  },
```

- [ ] **Passo 8: Build**

```bash
dotnet build Climato.slnx
```

Esperado: `Build succeeded`, 0 erros.

- [ ] **Passo 9: Smoke test (memória)** — subir o Provider sem Redis; `GET /weathers?cityName=Cuiabá` e depois `GET /weathers?cityName= cuiaba ` → o log da segunda **não** mostra `System.Net.Http.HttpClient.IWeatherAppService` chamando `api.openweathermap.org` (log `Information` do `HttpClient`). `/health/ready` → `Healthy` (sem check de Redis). Parar o serviço.

- [ ] **Passo 10: Commit** (após `/revisar-padrao`)

```bash
git add Directory.Packages.props src/Provider
git commit -m "Adiciona cache de clima e health check do Redis no Provider

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 3: Gateway — contrato tipado, erros, resiliência e health checks (A15, A19)

**Files:**
- Create: `Gateway/Climato.Gateway.Application/Exceptions/ProviderUnavailableException.cs`, `Gateway/Climato.Gateway.Infrastructure/Http/ProviderHealthCheck.cs`
- Modify: `Directory.Packages.props`, `Climato.Gateway.Infrastructure.csproj`, `Model/Weathers/WeatherDto.cs`, `Contracts/IWeatherAppService.cs`, `Contracts/IWeatherProviderClient.cs`, `Services/WeatherAppService.cs`, `Http/WeatherProviderClient.cs`, `Gateway/Climato.Gateway.Infrastructure/DependencyInjection.cs`, `Errors/GlobalExceptionHandler.cs`, `Controllers/WeatherController.cs`, `Program.cs`
- Delete: `Gateway/Climato.Gateway.Infrastructure/Http/JsonDictionary.cs`, `Http/HttpResponseMessageExtensions.cs`

**Interfaces:**
- Consumes: contrato JSON do Provider (Tarefa 1): `windSpeedKmh`, `sunrise`/`sunset` ISO 8601 com offset, 404 para cidade inexistente, 503 para falha externa; `/health/ready` do Provider (Tarefa 2).
- Produces: `WeatherDto` com `WindSpeedKmh` e `DateTimeOffset`; `IWeatherProviderClient.GetWeatherAsync(string, CancellationToken) : Task<WeatherDto?>`; `IWeatherAppService.GetByCityAsync(string, Guid, CancellationToken) : Task<WeatherDto?>`; `ProviderUnavailableException(string message, Exception? innerException = null)`.

- [ ] **Passo 1: Pacotes** — em `Directory.Packages.props`, remover `<PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />` e adicionar, em ordem alfabética (após `Microsoft.Extensions.Caching.StackExchangeRedis`):

```xml
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.13" />
```

Em `Climato.Gateway.Infrastructure.csproj`, trocar `<PackageReference Include="Newtonsoft.Json" />` e manter a ordem alfabética do `ItemGroup`:

```xml
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
    <PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

- [ ] **Passo 2: `Gateway/Climato.Gateway.Application/Model/Weathers/WeatherDto.cs` (reescrever)**

```csharp
namespace Climato.Gateway.Application.Model.Weathers;

public sealed class WeatherDto
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required double Temperature { get; init; }
    public required double FeelsLike { get; init; }
    public required double TempMin { get; init; }
    public required double TempMax { get; init; }
    public required int Humidity { get; init; }
    public required int Pressure { get; init; }
    public required double WindSpeedKmh { get; init; }
    public required int WindDirection { get; init; }
    public required DateTimeOffset Sunrise { get; init; }
    public required DateTimeOffset Sunset { get; init; }
    public string? ImageUrl { get; init; }
}
```

- [ ] **Passo 3: `Gateway/Climato.Gateway.Application/Exceptions/ProviderUnavailableException.cs`**

```csharp
namespace Climato.Gateway.Application.Exceptions;

public sealed class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Passo 4: Contratos** — `Contracts/IWeatherProviderClient.cs`:

```csharp
using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherProviderClient
{
    Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken);
}
```

`Contracts/IWeatherAppService.cs`:

```csharp
using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherAppService
{
    Task<WeatherDto?> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken);
}
```

- [ ] **Passo 5: `Gateway/Climato.Gateway.Application/Services/WeatherAppService.cs` (reescrever)** — inclui a pendência dos `?? throw` neste arquivo:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.Domain.Cities;

namespace Climato.Gateway.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IWeatherProviderClient weatherProviderClient;

    public WeatherAppService(ICityRepository repository, IWeatherProviderClient providerClient)
    {
        cityRepository = repository;
        weatherProviderClient = providerClient;
    }

    public async Task<WeatherDto?> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken)
    {
        var weather = await weatherProviderClient.GetWeatherAsync(cityName, cancellationToken);

        if (weather == null)
        {
            return null;
        }

        var city = await cityRepository.GetByNameAsync(weather.City, userId, cancellationToken);

        if (city == null)
        {
            await cityRepository.CreateAsync(new City(weather.City, weather.Country, userId), cancellationToken);
        }

        return weather;
    }
}
```

- [ ] **Passo 6: `Gateway/Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs` (reescrever)**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Exceptions;
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

    public async Task<WeatherDto?> GetWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync($"weathers?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderUnavailableException($"O Provider respondeu {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<WeatherDto>(cancellationToken)
                ?? throw new ProviderUnavailableException("O Provider respondeu sem corpo.");
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException or JsonException)
        {
            throw new ProviderUnavailableException("Falha ao chamar o Provider.", exception);
        }
    }
}
```

- [ ] **Passo 7: remover** `Gateway/Climato.Gateway.Infrastructure/Http/JsonDictionary.cs` e `Http/HttpResponseMessageExtensions.cs`.

- [ ] **Passo 8: `Gateway/Climato.Gateway.Infrastructure/Http/ProviderHealthCheck.cs`**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Climato.Gateway.Infrastructure.Http;

public sealed class ProviderHealthCheck : IHealthCheck
{
    public const string HttpClientName = "ProviderHealth";

    private readonly IHttpClientFactory httpClientFactory;

    public ProviderHealthCheck(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync("health/ready", cancellationToken);

        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"Provider respondeu {(int)response.StatusCode}.");
    }
}
```

(Exceção de rede ou timeout é capturada pelo próprio `HealthCheckService` e vira `Unhealthy` — sem `try/catch` aqui, GE-04.)

- [ ] **Passo 9: `Gateway/Climato.Gateway.Infrastructure/DependencyInjection.cs`** — trocar o bloco `services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>(...)` por:

```csharp
        services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>(ConfigureProviderClient)
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
                options.Retry.MaxRetryAttempts = 1;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            });

        services.AddHttpClient(ProviderHealthCheck.HttpClientName, (serviceProvider, client) =>
        {
            ConfigureProviderClient(serviceProvider, client);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHealthChecks()
            .AddDbContextCheck<ClimatoDbContext>("postgres", tags: ["ready"])
            .AddCheck<ProviderHealthCheck>("provider", tags: ["ready"]);

        return services;
    }

    private static void ConfigureProviderClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    }
```

(O `return services;` e a chave de fechamento do método antigo saem — o bloco acima já fecha `AddGatewayInfrastructure`.)

- [ ] **Passo 10: `Gateway/Climato.Gateway.Api/Errors/GlobalExceptionHandler.cs` (reescrever)** — versão com 503 e 499 (o 401 de `UserNotFoundException` entra na Tarefa 4):

```csharp
using Climato.Gateway.Application.Exceptions;
using Climato.Gateway.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Requisição cancelada pelo cliente em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        ProblemDetails problem;

        if (exception is DomainException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Operação não permitida.",
                Detail = exception.Message
            };
        }
        else if (exception is ProviderUnavailableException)
        {
            logger.LogWarning(exception, "Provider indisponível em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Serviço de clima indisponível. Tente novamente em instantes."
            };
        }
        else
        {
            logger.LogError(exception, "Erro não tratado em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno."
            };
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
```

- [ ] **Passo 11: `Gateway/Climato.Gateway.Api/Controllers/WeatherController.cs` (reescrever)**

```csharp
using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Weathers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("weathers")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna os dados climáticos da cidade informada e a registra no histórico do usuário autenticado.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Clima atual, com vento em km/h e horários no fuso da cidade.</response>
    /// <response code="404">Cidade não encontrada.</response>
    /// <response code="503">Serviço de clima indisponível.</response>
    [HttpGet("{cityName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WeatherDto>> GetWeather(string cityName, CancellationToken cancellationToken)
    {
        var weather = await appService.GetByCityAsync(cityName, User.GetUserId(), cancellationToken);

        if (weather == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cidade não encontrada.");
        }

        return Ok(weather);
    }
}
```

- [ ] **Passo 12: `Gateway/Climato.Gateway.Api/Program.cs`** — adicionar `using Microsoft.AspNetCore.Diagnostics.HealthChecks;` e, logo após `app.MapControllers();`:

```csharp
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
```

- [ ] **Passo 13: Build**

```bash
dotnet build Climato.slnx
```

Esperado: `Build succeeded`, 0 erros. `grep -rn "Newtonsoft\|JsonDictionary" src` → nenhuma ocorrência.

- [ ] **Passo 14: Smoke test** — subir Provider e Gateway; no Scalar do Gateway (`https://localhost:7001/scalar`), autenticado:
  - `GET /weathers/Tóquio` → 200 com `windSpeedKmh` e `sunrise` em `+09:00`.
  - `GET /weathers/cidadeinexistentexyz` → 404 ProblemDetails; `GET /cities` não lista essa cidade.
  - Parar o Provider → `GET /weathers/Lisboa` → 503 com o título em português; `GET /health/ready` (sem token) → 503 `Unhealthy`; `GET /health/live` → 200.

  Parar os serviços.

- [ ] **Passo 15: Commit** (após `/revisar-padrao`)

```bash
git add Directory.Packages.props src/Gateway
git commit -m "Tipa o cliente do Provider no Gateway e responde 503 quando ele falha

Resolve A15 e A19 no Gateway: WeatherDto lido com System.Text.Json, cidade
inexistente vira 404 sem entrar no histórico, resiliência no client do
Provider e health checks /health/live e /health/ready.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 4: Gateway — pendências de erro, repositório, construtores e nulabilidade

**Files:**
- Create: `Gateway/Climato.Gateway.Domain/Exceptions/UserNotFoundException.cs`
- Modify: `Data/Repositories/UserRepository.cs`, `Data/Repositories/CityRepository.cs`, `Services/CityAppService.cs`, `Gateway/Climato.Gateway.Api/Errors/GlobalExceptionHandler.cs`, `Gateway/Climato.Gateway.Api/DependencyInjection.cs`, `.editorconfig`

**Interfaces:**
- Consumes: `GlobalExceptionHandler` da Tarefa 3.
- Produces: `UserNotFoundException()` (Domain) → 401; `DomainException("E-mail já cadastrado.")` no cadastro concorrente → 409.

- [ ] **Passo 1: `Gateway/Climato.Gateway.Domain/Exceptions/UserNotFoundException.cs`**

```csharp
namespace Climato.Gateway.Domain.Exceptions;

public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("O usuário do token não existe mais.")
    {
    }
}
```

(Não herda de `DomainException`, para não cair no 409.)

- [ ] **Passo 2: `UserRepository.CreateAsync`** — adicionar `using Climato.Gateway.Domain.Exceptions;` e `using Npgsql;` e trocar o método por:

```csharp
    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ix_user_email" })
        {
            throw new DomainException("E-mail já cadastrado.");
        }
    }
```

- [ ] **Passo 3: `CityRepository`** — adicionar `using Climato.Gateway.Domain.Exceptions;` e `using Npgsql;`; trocar `CreateAsync` e `UpdateAsync` por:

```csharp
    public async Task CreateAsync(City city, CancellationToken cancellationToken)
    {
        await context.Cities.AddAsync(city, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "fk_city_user" })
        {
            throw new UserNotFoundException();
        }
    }

    public async Task UpdateAsync(City city, CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
```

(`UpdateAsync` mantém o parâmetro `city` porque faz parte de `ICityRepository`; o `UserRepository.UpdateAsync` já segue esse formato.)

- [ ] **Passo 4: `CityAppService`** — construtor sem `?? throw`:

```csharp
    public CityAppService(ICityRepository repository, IMapper mapper)
    {
        cityRepository = repository;
        this.mapper = mapper;
    }
```

Conferir com `grep -rn "?? throw new ArgumentNullException" src` → nenhuma ocorrência.

- [ ] **Passo 5: `GlobalExceptionHandler` do Gateway** — inserir entre o bloco `DomainException` e o `ProviderUnavailableException`:

```csharp
        else if (exception is UserNotFoundException)
        {
            logger.LogInformation("Token de usuário inexistente em {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sessão inválida."
            };
        }
```

- [ ] **Passo 6: `Gateway/Climato.Gateway.Api/DependencyInjection.cs`** — adicionar `using Microsoft.AspNetCore.Mvc;` e `using Microsoft.AspNetCore.Mvc.Infrastructure;` e trocar a linha `services.AddControllers(options => options.Filters.Add<ValidationFilter>());` por:

```csharp
        services.AddControllers(options => options.Filters.Add<ValidationFilter>())
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problem = factory.CreateValidationProblemDetails(
                        context.HttpContext,
                        context.ModelState,
                        StatusCodes.Status400BadRequest,
                        "Dados inválidos.");

                    return new BadRequestObjectResult(problem);
                };
            });
```

- [ ] **Passo 7: `.editorconfig`** — remover as seis linhas (comentário + regra) de CS8601, CS8604 e CS8613, mantendo a de IDE0008.

- [ ] **Passo 8: Build sem avisos de nulabilidade**

```bash
dotnet build Climato.slnx --no-incremental
```

Esperado: `Build succeeded`, 0 erros e nenhum aviso `CS86xx` (antes da fase, com as supressões removidas, todos os avisos vinham de `JsonDictionary`, `Weather`, `WeatherDto`, `WeatherProviderClient` e `WeatherAppService` do Provider — todos reescritos ou removidos nas Tarefas 1 e 3). Se aparecer algum, corrigir no arquivo apontado antes de seguir.

- [ ] **Passo 9: Smoke test**
  - `POST /auth/login` com corpo `{` → 400 com `"title": "Dados inválidos."`.
  - Cadastrar um usuário, apagar a conta com `DELETE /users/me` e, com o mesmo token, `GET /weathers/Recife` → 401 "Sessão inválida.".

- [ ] **Passo 10: Commit** (após `/revisar-padrao`)

```bash
git add .editorconfig src/Gateway
git commit -m "Trata cadastro concorrente, token órfão e 400 automático no Gateway

Remove as supressões de nulabilidade, os ArgumentNullException impossíveis e o
Update() redundante do CityRepository (pendências da Fase 03).

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 5: Front — vento, horário local e erro distinto de "não encontrada" (A15, A16, A17)

**Files:**
- Modify: `web/src/components/formatDatetime.jsx`, `web/src/pages/WeatherDetails.jsx`

**Interfaces:**
- Consumes: `windSpeedKmh` e `sunrise`/`sunset` ISO 8601 com offset (Tarefa 3); `getApiErrorMessage(error, fallback)` de `services/api.js`.

- [ ] **Passo 1: `web/src/components/formatDatetime.jsx` (reescrever)**

```jsx
const formatDatetime = (isoWithOffset) => isoWithOffset.slice(11, 16);

export default formatDatetime;
```

(`"2026-10-19T05:52:00+09:00"` → `"05:52"`, já no fuso da cidade; `new Date()` reconverteria para o fuso do navegador.)

- [ ] **Passo 2: `WeatherDetails.jsx` — import e estado** — trocar `import { getWeatherByCity } from "../services/api";` por:

```jsx
import { getApiErrorMessage, getWeatherByCity } from "../services/api";
```

e, após `const [notFound, setNotFound] = useState(false);`:

```jsx
  const [errorMessage, setErrorMessage] = useState(null);
```

- [ ] **Passo 3: `WeatherDetails.jsx` — `catch`** — trocar

```jsx
      } catch (error) {
        setNotFound(true);
      } finally {
```

por:

```jsx
      } catch (error) {
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
          setErrorMessage(
            getApiErrorMessage(error, "Não foi possível carregar o clima agora.")
          );
        }
      } finally {
```

- [ ] **Passo 4: `WeatherDetails.jsx` — render** — trocar `{notFound ? (` por `{notFound || errorMessage ? (` e o `description` do `Empty` por:

```jsx
              description={
                <Typography.Text>
                  {notFound ? (
                    <>
                      Cidade "<strong>{cityName}</strong>" não encontrada.
                    </>
                  ) : (
                    errorMessage
                  )}
                </Typography.Text>
              }
```

- [ ] **Passo 5: `WeatherDetails.jsx` — vento** — trocar `{weather.windSpeed} km/h` por `{weather.windSpeedKmh} km/h`.

- [ ] **Passo 6: Lint e build**

```bash
npm --prefix web run lint
npm --prefix web run build
```

Esperado: lint sem erros; `✓ built in ...`.

- [ ] **Passo 7: Smoke test** — com Provider, Gateway e web no ar: buscar "Tóquio" → nascer/pôr do sol batem com o horário local de Tóquio (conferir em [timeanddate.com](https://www.timeanddate.com/sun/japan/tokyo)), vento em km/h coerente; buscar "cidadeinexistentexyz" → "não encontrada"; parar o Provider e buscar "Lisboa" → mensagem de indisponibilidade (não "não encontrada").

- [ ] **Passo 8: Commit** (após `/revisar-padrao`)

```bash
git add web/src
git commit -m "Exibe vento em km/h, horários locais da cidade e erro de indisponibilidade no front

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 6: Documentação de arquitetura, runbook e checklist

**Files:**
- Modify: `docs/architecture/backend.md`, `docs/architecture/system-design.md`, `docs/runbook.md`, `docs/architecture/review-checklist.md`

- [ ] **Passo 1: `backend.md` — Stack** — na tabela:
  - Linha `JSON externo`: `| JSON externo | System.Text.Json + DTOs tipados por API externa (`ReadFromJsonAsync`) | — |`
  - Linha `Resiliência / cache`: `| Resiliência / cache | `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`) / `IDistributedCache` (Redis opcional, memória sem connection string) no Provider | — |`
  - Nova linha após ela: `| Health checks | `/health/live` e `/health/ready` (`Microsoft.Extensions.Diagnostics.HealthChecks`) | — |`

- [ ] **Passo 2: `backend.md` — Estrutura da Solução** — trocar as linhas do Provider e acrescentar `Exceptions` no Gateway:

```
├── Climato.Gateway.Application/     ← Contracts, Services, Model (DTOs: Auth, Cities, Users, Weathers), Mappings (Mapster), Validators, Options, Exceptions, DependencyInjection.cs
...
└── Climato.Gateway.Infrastructure/  ← Data (ClimatoDbContext, Configurations, Migrations, Repositories), Http (clients, ProviderHealthCheck), Auth (JwtTokenService), DependencyInjection.cs
src/Provider/
├── Climato.Provider.Api/            ← Program.cs, DependencyInjection.cs, Controllers, Errors
├── Climato.Provider.Application/    ← Clientes das APIs externas e cache (Services), Contracts, Model (DTOs da OpenWeather/Pexels), Exceptions, Options, DependencyInjection.cs
└── Climato.Provider.Domain/         ← Weather (contrato devolvido ao Gateway)
```

- [ ] **Passo 3: `backend.md` — Comunicação HTTP** — trocar o item `**(a partir da Fase 03)** DTOs tipados ...` por:

```markdown
- DTOs tipados por API externa (`OpenWeatherCurrentResponse`, `PexelsSearchResponse`) em `Application/Model/<Api>/`, lidos com `ReadFromJsonAsync<T>`. Nada de `Dictionary<string, object>`, `JObject` ou Newtonsoft.
- `using` em todo `HttpResponseMessage` (e `HttpRequestMessage`, quando criado à mão). Headers fixos (ex.: `Authorization` do Pexels) vão em `DefaultRequestHeaders` no registro do client.
- Resiliência com `AddStandardResilienceHandler()` em todo typed client. Timeouts escalonados: a tentativa de quem chama é maior que o timeout total de quem é chamado (Gateway → Provider 20 s/25 s; Provider → OpenWeather 5 s/15 s; Provider → Pexels 3 s/5 s). `CircuitBreaker.SamplingDuration` ≥ 2× o timeout por tentativa.
- Client traduz a resposta: 404 → `null`; outro status de erro, `HttpRequestException`, `ExecutionRejectedException` (Polly) ou `JsonException` → exceção tipada de indisponibilidade (`ExternalServiceUnavailableException` no Provider, `ProviderUnavailableException` no Gateway).
```

- [ ] **Passo 4: `backend.md` — Erros** — acrescentar ao fim da seção:

```markdown
- Dependência fora (API externa, Provider) → exceção tipada de indisponibilidade → **503** no `GlobalExceptionHandler`, log `Warning`. Cidade inexistente continua sendo fluxo esperado (`null` → 404).
- Cliente abortou (`OperationCanceledException` com `RequestAborted` cancelado) → 499 sem corpo, log `Information`.
- Violação de constraint que reflete regra (e-mail único) → o repositório traduz para `DomainException` (409); FK de usuário inexistente → `UserNotFoundException` → 401.
- 400 automático do `[ApiController]` usa o mesmo `ValidationProblemDetails` do `ValidationFilter` (`InvalidModelStateResponseFactory`, título "Dados inválidos.").
- Falha opcional (imagem do Pexels, cache) não derruba a resposta: log `Warning` e segue.
```

- [ ] **Passo 5: `backend.md` — novas seções** — antes de `### Configuração e segredos`:

```markdown
### Cache (Provider)
- `IDistributedCache` atrás de `IWeatherCache` (`Services/WeatherCache`). Redis se `ConnectionStrings:Redis` estiver preenchida; senão `AddDistributedMemoryCache`.
- Chave `clima:{cidade-normalizada}` (trim, espaços colapsados, minúsculas, sem acentos); valor em JSON; TTL absoluto de 10 min.
- Só resposta de sucesso entra no cache (404 e falhas não). Erro do cache → `Warning` e segue sem cache.

### Health checks
- `/health/live`: sem checks (processo vivo). `/health/ready`: checks com a tag `ready`.
- Gateway: Postgres (`AddDbContextCheck`) e Provider (`ProviderHealthCheck` → `GET health/ready`, client nomeado sem resiliência, 5 s). Provider: Redis, só se configurado.
- APIs externas (OpenWeather, Pexels) **não** entram: gastariam cota.
- No Gateway, `MapHealthChecks(...).AllowAnonymous()`.
```

- [ ] **Passo 6: `backend.md` — Injeção de dependência** — trocar `(`AddGatewayApplication`, `AddGatewayInfrastructure(configuration)`, `AddProviderApplication`)` por `(`AddGatewayApi(configuration)`, `AddGatewayApplication`, `AddGatewayInfrastructure(configuration)`, `AddProviderApi(configuration)`, `AddProviderApplication(configuration)`)`.

- [ ] **Passo 7: `system-design.md`**
  - Título `### Consulta de clima (alvo)` → `### Consulta de clima`; no passo 3, `clima:{cidade-normalizada}` com "(TTL 10 min; Redis ou memória)".
  - Título `### Mapeamento de erros (alvo, Fase 03)` → `### Mapeamento de erros`; tabela:

```markdown
| Situação | Provider responde | Gateway responde |
|---|---|---|
| Cidade não existe na OpenWeather | 404 | 404 (não entra no histórico) |
| OpenWeather fora / timeout / circuito aberto / 401 de chave / 429 | 503 | 503 |
| Pexels falha | 200 sem `imageUrl` | 200 |
| Provider fora ou circuito aberto | — | 503 |
| Cliente abortou a requisição | 499 | 499 |
```

  - Nova subseção após a tabela:

```markdown
### Health checks

| Serviço | `/health/live` | `/health/ready` |
|---|---|---|
| Provider | processo vivo | Redis (se configurado) |
| Gateway | processo vivo | Postgres + `/health/ready` do Provider |
```

- [ ] **Passo 8: `runbook.md`**
  - Em Pré-requisitos, adicionar: `- (Opcional) Redis em `localhost:6379` — sem ele o Provider usa cache em memória`.
  - Nova seção `## 3a. Redis (opcional)` após a seção 3:

````markdown
## 3a. Redis (opcional)

Sem `ConnectionStrings:Redis`, o Provider usa cache em memória (por processo). Para usar Redis:

```bash
docker run -d --name climato-redis -p 6379:6379 redis:7-alpine
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379,abortConnect=false,connectTimeout=1000" --project src/Provider/Climato.Provider.Api
```

`abortConnect=false,connectTimeout=1000` evita que cada requisição espere 5 s quando o Redis cai: o Provider loga um warning e segue sem cache.
````

  - Na tabela da seção 4, nova coluna ou linhas: `Health: https://localhost:7002/health/ready` (Provider) e `https://localhost:7001/health/ready` (Gateway); `/health/live` em ambos.
  - Em Problemas comuns, trocar a linha "Toda busca mostra 'cidade não encontrada'" por:

```markdown
| Busca mostra "Serviço de clima indisponível" | Provider fora do ar, chave da OpenWeather inválida ou cota estourada — ver o log do Provider e `https://localhost:7001/health/ready` |
| Warning `Falha ao ler o cache de clima` no log do Provider | Redis configurado mas fora do ar — subir o container ou remover `ConnectionStrings:Redis` |
```

- [ ] **Passo 9: `review-checklist.md`** — novas linhas no Backend (IDs novos, nunca reutilizar):

```markdown
| BE-24 | 🟡 | 03 | Typed client sem resiliência | `AddHttpClient<...>()` sem `AddStandardResilienceHandler()` (exceto client nomeado de health check) | [Comunicação HTTP](backend.md#comunicação-http) |
| BE-25 | 🟡 | 03 | `HttpResponseMessage` sem `using` | `await client.GetAsync/SendAsync(...)` atribuído sem `using` | [Comunicação HTTP](backend.md#comunicação-http) |
| BE-26 | 🟡 | 03 | Falha de dependência como 404/500 | `catch` de `HttpRequestException` devolvendo 404, ou `EnsureSuccessStatusCode()` deixando o erro do serviço chamado virar 500 | [Erros](backend.md#erros) |
| BE-27 | 🟡 | 03 | Falha opcional derrubando a resposta | Erro de imagem/cache propagado em vez de `Warning` + resposta sem o dado opcional | [Erros](backend.md#erros) |
```

- [ ] **Passo 10: Commit** (após `/revisar-padrao`)

```bash
git add docs/architecture docs/runbook.md
git commit -m "Documenta contrato tipado, resiliência, cache e health checks

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 7: Auditoria e project-overview

**Files:**
- Modify: `docs/features/00-roteiro-portfolio/auditoria-inicial.md`, `docs/project-overview.md`

- [ ] **Passo 1: `auditoria-inicial.md`** — coluna Status de A14, A15, A16, A17 e A19 → `✅ Fase 03`.

- [ ] **Passo 2: `project-overview.md` — Fase 03** — trocar o bloco da fase por:

```markdown
#### Fase 03 — Comunicação entre serviços
Dividida em dois blocos ([design 03a](features/03-comunicacao-servicos/design.md)):
- **03a — Contrato e robustez** ✅ (AAAA-MM-DD do merge): DTOs tipados, erros 404/503, resiliência, cache (Redis opcional), health checks, vento em km/h e horários no fuso da cidade.
- **03b — Novos endpoints**: previsão de 5 dias e busca de cidades (geocoding).

Auditoria: A14–A17, A19 (resolvidas na 03a).
```

  (A fase só recebe `✅ Concluída` no título ao fim da 03b.)

- [ ] **Passo 3: `project-overview.md` — Estado atual** — título `## Estado Atual (AAAA-MM-DD, após a Fase 03a)`; em **Funciona**, acrescentar "erros distintos para cidade inexistente (404) e serviço indisponível (503), cache de clima e health checks"; em **Stack**, trocar `Newtonsoft.Json (até a Fase 03)` por `Microsoft.Extensions.Http.Resilience · IDistributedCache (Redis opcional)`.

- [ ] **Passo 4: `project-overview.md` — Pendências** — remover as quatro linhas `[Fase 03]` (checagens impossíveis, nulabilidade, erros, 400 automático).

- [ ] **Passo 5: Commit** (após `/revisar-padrao`)

```bash
git add docs/features/00-roteiro-portfolio/auditoria-inicial.md docs/project-overview.md
git commit -m "Marca A14–A17 e A19 e registra a conclusão da Fase 03a

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 8: Verificação final e PR

- [ ] **Passo 1:** `/verification-before-completion` com os comandos da [Verificação](../../../CLAUDE.md#verificação):

```bash
dotnet build Climato.slnx --no-incremental
npm --prefix web run lint
npm --prefix web run build
```

Esperado: build sem erros e sem avisos `CS86xx`; lint limpo; build do web ok.

- [ ] **Passo 2:** roteiro manual da seção [Verificação do design](design.md#verificação) (itens 1–12), anotando o resultado de cada um no PR. Para o item 9, subir o Redis conforme o runbook (seção 3a), conferir uma busca, parar o container e repetir.

- [ ] **Passo 3:** `/revisar-padrao` no diff completo da branch → sem 🔴 (ou liberados pelo usuário).

- [ ] **Passo 4:** `/requesting-code-review`.

- [ ] **Passo 5:** `/finishing-a-development-branch` → PR `dev/contrato-robustez` → `master`, citando A14, A15, A16, A17, A19 e as pendências resolvidas; trocar o `AAAA-MM-DD` da Tarefa 7 pela data real no commit final.
