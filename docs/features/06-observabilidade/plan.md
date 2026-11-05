# Fase 06 — Observabilidade: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`): marcar ao concluir (regra GE-05).

**Objetivo:** logs Serilog estruturados (JSON fora de Development), correlation ID = TraceId W3C devolvido em `X-Correlation-ID` e no `ProblemDetails`, e traces OpenTelemetry exportados para o Jaeger no compose, conforme o [design aprovado](design.md).

**Arquitetura:** primeiro o Provider (regras com TDD, depois a ligação de Serilog e OTel), em seguida o Gateway (as mesmas regras mais o `CorrelationIdMiddleware`, depois a ligação com Npgsql e CORS). Por fim o compose com o Jaeger, a verificação manual de ponta a ponta e a documentação. Cada tarefa termina verificada e vira um commit.

**Stack:** .NET 10 · Serilog.AspNetCore 10.0.0 · OpenTelemetry 1.20.1 · Npgsql.OpenTelemetry 10.0.3 · Jaeger v2 (`jaegertracing/jaeger:2.22.0`) · xUnit v3 + Shouldly.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) antes de editar. Em caso de conflito, vale o design.
- O código novo fica só em `*.Api` (`Observability/` e `Errors/`). Application, Domain e Infrastructure não mudam, e os `logger.Log*` existentes continuam como estão: o Serilog entra por baixo do `ILogger<T>`.
- **Não desligar a redação de URI do `HttpClient`.** A chave da OpenWeather vai em `appid=`.
- Versões só no `Directory.Packages.props`, e `.csproj` sem `Version` (BE-21). Conferir o namespace de cada pacote novo no próprio pacote antes de usar (CLAUDE.md).
- **Nenhum segredo versionado** (BE-01). Nos testes manuais, não ler o `.env`, os user-secrets nem o `appsettings.Development.json` do usuário: só usar o `.env` que já existe, sem abrir o arquivo, ou chaves fictícias (`chave-ficticia`).
- **Porta 8080 do host ocupada** por outro projeto (`statement-importer`). Não subir o serviço `web` do compose e não mexer nesses containers. A verificação manual usa `gateway`, `provider`, `postgres`, `redis` e `jaeger`.
- Nomes de teste `Metodo_Cenario_ResultadoEsperado` em português; Arrange/Act/Assert separados por linha em branco.
- **Commits:** um por tarefa (0–6), com as mensagens indicadas. A aprovação deste plano é a autorização explícita para esses commits (mesma exceção das Fases 01–05). Antes de cada commit, `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, no imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/observabilidade`, a partir do `origin/master` (com as Fases 04 e 05), sem upstream.
- Fora do escopo: métricas OTel, logs via OTLP, instrumentação do Redis, sinks externos, mudanças no web.

## Mapa de arquivos

| Arquivo | Ação | Tarefa |
|---|---|---|
| `docs/features/06-observabilidade/{design,plan}.md` | Criar (já escritos) | 0 |
| `Directory.Packages.props` | Modificar | 1 |
| `src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj` | Modificar | 1 |
| `src/Provider/Climato.Provider.Api/Errors/ProblemDetailsTraceId.cs` | Criar | 1 |
| `src/Provider/Climato.Provider.Api/Observability/RequestLogLevel.cs` | Criar | 1 |
| `tests/Climato.Provider.UnitTests/Climato.Provider.UnitTests.csproj` | Modificar | 1 |
| `tests/Climato.Provider.UnitTests/Api/Errors/ProblemDetailsTraceIdTests.cs` | Criar | 1 |
| `tests/Climato.Provider.UnitTests/Api/Observability/RequestLogLevelTests.cs` | Criar | 1 |
| `src/Provider/Climato.Provider.Api/Observability/ObservabilityExtensions.cs` | Criar | 2 |
| `src/Provider/Climato.Provider.Api/DependencyInjection.cs` | Modificar | 2 |
| `src/Provider/Climato.Provider.Api/Program.cs` | Modificar | 2 |
| `src/Provider/Climato.Provider.Api/appsettings.json` | Modificar | 2 |
| `src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj` | Modificar | 3 |
| `src/Gateway/Climato.Gateway.Api/Errors/ProblemDetailsTraceId.cs` | Criar | 3 |
| `src/Gateway/Climato.Gateway.Api/Observability/RequestLogLevel.cs` | Criar | 3 |
| `src/Gateway/Climato.Gateway.Api/Observability/CorrelationIdMiddleware.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Api/Errors/ProblemDetailsTraceIdTests.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Api/Observability/RequestLogLevelTests.cs` | Criar | 3 |
| `tests/Climato.Gateway.UnitTests/Api/Observability/CorrelationIdMiddlewareTests.cs` | Criar | 3 |
| `src/Gateway/Climato.Gateway.Api/Observability/ObservabilityExtensions.cs` | Criar | 4 |
| `src/Gateway/Climato.Gateway.Api/DependencyInjection.cs` | Modificar | 4 |
| `src/Gateway/Climato.Gateway.Api/Program.cs` | Modificar | 4 |
| `src/Gateway/Climato.Gateway.Api/appsettings.json` | Modificar | 4 |
| `docker-compose.yml`, `.env.example` | Modificar | 5 |
| `docs/architecture/{backend,review-checklist,system-design}.md`, `docs/runbook.md` | Modificar | 6 |
| `docs/features/00-roteiro-portfolio/auditoria-inicial.md`, `docs/project-overview.md` | Modificar | 6 |

---

### Tarefa 0: Documentos da fase

- [ ] **Passo 1:** conferir que `docs/features/06-observabilidade/design.md` e `plan.md` existem e que o usuário aprovou o plano.
- [ ] **Passo 2:** `/revisar-padrao` e commit.

```bash
git add docs/features/06-observabilidade/design.md docs/features/06-observabilidade/plan.md
git commit -m "Adiciona design e plano da Fase 06 (observabilidade)"
```

---

### Tarefa 1: Provider: `traceId` do ProblemDetails e nível do request log (TDD)

**Arquivos:** ver o mapa (tarefa 1).

**Interfaces produzidas:**
- `Climato.Provider.Api.Errors.ProblemDetailsTraceId.Apply(ProblemDetailsContext context)`: `static void`, usada como `CustomizeProblemDetails`.
- `Climato.Provider.Api.Observability.RequestLogLevel.Get(HttpContext context, double elapsedMilliseconds, Exception? exception)`: `static LogEventLevel`, usada como `GetLevel` do `UseSerilogRequestLogging`.

- [ ] **Passo 1: pacotes.** Em `Directory.Packages.props`, dentro do `<ItemGroup>` e em ordem alfabética:

```xml
    <PackageVersion Include="Npgsql.OpenTelemetry" Version="10.0.3" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.20.1" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.20.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.20.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.20.1" />
    <PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
```

Em `src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj`, no `ItemGroup` de pacotes:

```xml
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="Serilog.AspNetCore" />
```

Em `tests/Climato.Provider.UnitTests/Climato.Provider.UnitTests.csproj`, trocar a referência da Application pela da Api. A Api referencia a Application, então os testes atuais continuam compilando:

```xml
    <ProjectReference Include="..\..\src\Provider\Climato.Provider.Api\Climato.Provider.Api.csproj" />
```

- [ ] **Passo 2: testes que falham.** Criar `tests/Climato.Provider.UnitTests/Api/Errors/ProblemDetailsTraceIdTests.cs`:

```csharp
using System.Diagnostics;
using Climato.Provider.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.UnitTests.Api.Errors;

public class ProblemDetailsTraceIdTests
{
    [Fact]
    public void Apply_ComActivity_GravaOTraceIdPuro()
    {
        using var activity = new Activity("teste").Start();
        var context = CreateContext();

        ProblemDetailsTraceId.Apply(context);

        context.ProblemDetails.Extensions["traceId"].ShouldBe(activity.TraceId.ToHexString());
    }

    [Fact]
    public void Apply_SemActivity_GravaOTraceIdentifier()
    {
        Activity.Current = null;
        var context = CreateContext();
        context.HttpContext.TraceIdentifier = "0HN-teste";

        ProblemDetailsTraceId.Apply(context);

        context.ProblemDetails.Extensions["traceId"].ShouldBe("0HN-teste");
    }

    private static ProblemDetailsContext CreateContext() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ProblemDetails = new ProblemDetails()
    };
}
```

Criar `tests/Climato.Provider.UnitTests/Api/Observability/RequestLogLevelTests.cs`:

```csharp
using Climato.Provider.Api.Observability;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace Climato.Provider.UnitTests.Api.Observability;

public class RequestLogLevelTests
{
    [Fact]
    public void Get_HealthCheckComFalha_DevolveVerbose()
    {
        var context = CreateContext("/health/ready", StatusCodes.Status503ServiceUnavailable);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Verbose);
    }

    [Fact]
    public void Get_ComExcecao_DevolveError()
    {
        var context = CreateContext("/weathers", StatusCodes.Status200OK);

        var level = RequestLogLevel.Get(context, 1, new InvalidOperationException("falha"));

        level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void Get_Status500_DevolveError()
    {
        var context = CreateContext("/weathers", StatusCodes.Status500InternalServerError);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Error);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status499ClientClosedRequest)]
    public void Get_StatusAbaixoDe500_DevolveInformation(int statusCode)
    {
        var context = CreateContext("/weathers", statusCode);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Information);
    }

    private static DefaultHttpContext CreateContext(string path, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;
        return context;
    }
}
```

- [ ] **Passo 3: confirmar a falha.**

Run: `dotnet test --project tests/Climato.Provider.UnitTests`
Expected: erro de compilação (`ProblemDetailsTraceId` e `RequestLogLevel` não existem).

- [ ] **Passo 4: implementação mínima.** Criar `src/Provider/Climato.Provider.Api/Errors/ProblemDetailsTraceId.cs`:

```csharp
using System.Diagnostics;

namespace Climato.Provider.Api.Errors;

public static class ProblemDetailsTraceId
{
    public static void Apply(ProblemDetailsContext context)
    {
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;
    }
}
```

Criar `src/Provider/Climato.Provider.Api/Observability/RequestLogLevel.cs`:

```csharp
using Serilog.Events;

namespace Climato.Provider.Api.Observability;

public static class RequestLogLevel
{
    public static LogEventLevel Get(HttpContext context, double elapsedMilliseconds, Exception? exception)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Verbose;
        }

        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return LogEventLevel.Information;
    }
}
```

- [ ] **Passo 5: confirmar que passa.**

Run: `dotnet test --project tests/Climato.Provider.UnitTests`
Expected: todos verdes (os anteriores + 8 novos).

- [ ] **Passo 6:** `/revisar-padrao` e commit.

```bash
git add Directory.Packages.props src/Provider/Climato.Provider.Api tests/Climato.Provider.UnitTests
git commit -m "Adiciona o traceId puro no ProblemDetails e o nível do request log no Provider"
```

---

### Tarefa 2: Provider: ligar Serilog e OpenTelemetry

**Interfaces consumidas:** `ProblemDetailsTraceId.Apply` e `RequestLogLevel.Get` (Tarefa 1).
**Interfaces produzidas:** `Climato.Provider.Api.Observability.ObservabilityExtensions.AddObservability(this WebApplicationBuilder builder)`: devolve `WebApplicationBuilder`.

- [ ] **Passo 1:** criar `src/Provider/Climato.Provider.Api/Observability/ObservabilityExtensions.cs`:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace Climato.Provider.Api.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "Climato.Provider";
    private const string DevelopmentTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", ServiceName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName);

            if (environment.IsDevelopment())
            {
                logger.WriteTo.Console(outputTemplate: DevelopmentTemplate);
            }
            else
            {
                logger.WriteTo.Console(new CompactJsonFormatter());
            }
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    tracing.AddOtlpExporter();
                }
            });

        return builder;
    }
}
```

- [ ] **Passo 2:** em `src/Provider/Climato.Provider.Api/DependencyInjection.cs`, trocar `services.AddProblemDetails();` por:

```csharp
        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsTraceId.Apply);
```

- [ ] **Passo 3:** em `src/Provider/Climato.Provider.Api/Program.cs`:

```csharp
using Climato.Provider.Api;
using Climato.Provider.Api.Observability;
using Climato.Provider.Application;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.Services
    .AddProviderApi(builder.Configuration)
    .AddProviderApplication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel.Get);
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
```

- [ ] **Passo 4:** em `src/Provider/Climato.Provider.Api/appsettings.json`, trocar a seção `Logging` por:

```json
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    }
  },
```

- [ ] **Passo 5: build e testes.**

Run: `dotnet build Climato.slnx` → 0 erros, 0 avisos novos.
Run: `dotnet test --solution Climato.slnx` → todos verdes.

- [ ] **Passo 6: verificação manual (Production, sem segredo real).** Num terminal em background:

```bash
ASPNETCORE_ENVIRONMENT=Production OpenWeather__ApiKey=chave-ficticia Pexels__ApiKey=chave-ficticia dotnet run --project src/Provider/Climato.Provider.Api --no-launch-profile --urls http://localhost:5182
```

Depois:
- `curl -s http://localhost:5182/health/live`: `Healthy`, sem linha de request log no console.
- `curl -s -i "http://localhost:5182/weathers?cityName=Londres"`: 503 `ProblemDetails`, porque a chave fictícia leva a OpenWeather a responder 401. O `traceId` tem 32 hex, sem `00-` nem `-01`.
- No console: uma linha JSON por evento, com `@tr` igual ao `traceId` da resposta, `Service: "Climato.Provider"`, a linha `HTTP GET /weathers/... responded 503` e o `Warning` do `GlobalExceptionHandler`. Nenhuma URL com `appid=` aparece. Se o `traceId` do `ProblemDetails` não for o TraceId puro (o writer padrão gravou depois do `Customize`), parar e investigar com `systematic-debugging`.
- Encerrar o processo.

- [ ] **Passo 7:** `/revisar-padrao` e commit.

```bash
git add src/Provider/Climato.Provider.Api
git commit -m "Adiciona Serilog e OpenTelemetry no Provider"
```

---

### Tarefa 3: Gateway: `traceId`, nível do request log e `CorrelationIdMiddleware` (TDD)

**Interfaces produzidas:**
- `Climato.Gateway.Api.Errors.ProblemDetailsTraceId.Apply(ProblemDetailsContext context)`: `static void`.
- `Climato.Gateway.Api.Observability.RequestLogLevel.Get(HttpContext, double, Exception?)`: `static LogEventLevel`.
- `Climato.Gateway.Api.Observability.CorrelationIdMiddleware`: middleware por convenção (`ctor(RequestDelegate next)`, `Task InvokeAsync(HttpContext context)`), com `public const string HeaderName = "X-Correlation-ID"`.

- [ ] **Passo 1: pacotes.** Em `src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj`, no `ItemGroup` de pacotes:

```xml
    <PackageReference Include="Npgsql.OpenTelemetry" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="Serilog.AspNetCore" />
```

- [ ] **Passo 2: testes que falham.** Criar `tests/Climato.Gateway.UnitTests/Api/Errors/ProblemDetailsTraceIdTests.cs`:

```csharp
using System.Diagnostics;
using Climato.Gateway.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.UnitTests.Api.Errors;

public class ProblemDetailsTraceIdTests
{
    [Fact]
    public void Apply_ComActivity_GravaOTraceIdPuro()
    {
        using var activity = new Activity("teste").Start();
        var context = CreateContext();

        ProblemDetailsTraceId.Apply(context);

        context.ProblemDetails.Extensions["traceId"].ShouldBe(activity.TraceId.ToHexString());
    }

    [Fact]
    public void Apply_SemActivity_GravaOTraceIdentifier()
    {
        Activity.Current = null;
        var context = CreateContext();
        context.HttpContext.TraceIdentifier = "0HN-teste";

        ProblemDetailsTraceId.Apply(context);

        context.ProblemDetails.Extensions["traceId"].ShouldBe("0HN-teste");
    }

    private static ProblemDetailsContext CreateContext() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ProblemDetails = new ProblemDetails()
    };
}
```

Criar `tests/Climato.Gateway.UnitTests/Api/Observability/RequestLogLevelTests.cs`:

```csharp
using Climato.Gateway.Api.Observability;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace Climato.Gateway.UnitTests.Api.Observability;

public class RequestLogLevelTests
{
    [Fact]
    public void Get_HealthCheckComFalha_DevolveVerbose()
    {
        var context = CreateContext("/health/ready", StatusCodes.Status503ServiceUnavailable);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Verbose);
    }

    [Fact]
    public void Get_ComExcecao_DevolveError()
    {
        var context = CreateContext("/cities", StatusCodes.Status200OK);

        var level = RequestLogLevel.Get(context, 1, new InvalidOperationException("falha"));

        level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void Get_Status503_DevolveError()
    {
        var context = CreateContext("/weathers/londres", StatusCodes.Status503ServiceUnavailable);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Error);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status404NotFound)]
    public void Get_StatusAbaixoDe500_DevolveInformation(int statusCode)
    {
        var context = CreateContext("/cities", statusCode);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Information);
    }

    private static DefaultHttpContext CreateContext(string path, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;
        return context;
    }
}
```

Criar `tests/Climato.Gateway.UnitTests/Api/Observability/CorrelationIdMiddlewareTests.cs`. O `DefaultHttpContext` não dispara `OnStarting`, então o teste usa um `IHttpResponseFeature` que guarda os callbacks e os executa em `StartAsync`:

```csharp
using System.Diagnostics;
using Climato.Gateway.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Climato.Gateway.UnitTests.Api.Observability;

public class CorrelationIdMiddlewareTests
{
    private readonly StartingResponseFeature responseFeature = new();
    private readonly DefaultHttpContext httpContext = new();
    private bool nextCalled;

    public CorrelationIdMiddlewareTests()
    {
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);
    }

    [Fact]
    public async Task InvokeAsync_ComActivity_GravaOTraceIdNoHeaderAoIniciarAResposta()
    {
        using var activity = new Activity("teste").Start();
        var middleware = new CorrelationIdMiddleware(Next);

        await middleware.InvokeAsync(httpContext);
        await responseFeature.StartAsync();

        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().ShouldBe(activity.TraceId.ToHexString());
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_SemActivity_NaoGravaHeaderEChamaONext()
    {
        Activity.Current = null;
        var middleware = new CorrelationIdMiddleware(Next);

        await middleware.InvokeAsync(httpContext);
        await responseFeature.StartAsync();

        httpContext.Response.Headers.ContainsKey(CorrelationIdMiddleware.HeaderName).ShouldBeFalse();
        nextCalled.ShouldBeTrue();
    }

    private Task Next(HttpContext context)
    {
        nextCalled = true;
        return Task.CompletedTask;
    }

    private sealed class StartingResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> callbacks = [];

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            callbacks.Add((callback, state));
        }

        public async Task StartAsync()
        {
            foreach (var (callback, state) in callbacks)
            {
                await callback(state);
            }
        }
    }
}
```

Se `HttpResponseFeature` estiver marcado como obsoleto no .NET 10 (aviso CS0618), implementar `IHttpResponseFeature` diretamente na classe aninhada (`StatusCode`, `ReasonPhrase`, `Headers = new HeaderDictionary()`, `Body = Stream.Null`, `HasStarted => false`, `OnCompleted` vazio), mantendo o mesmo `OnStarting`/`StartAsync`.

- [ ] **Passo 3: confirmar a falha.**

Run: `dotnet test --project tests/Climato.Gateway.UnitTests`
Expected: erro de compilação (`ProblemDetailsTraceId`, `RequestLogLevel` e `CorrelationIdMiddleware` não existem).

- [ ] **Passo 4: implementação mínima.** Criar `src/Gateway/Climato.Gateway.Api/Errors/ProblemDetailsTraceId.cs`:

```csharp
using System.Diagnostics;

namespace Climato.Gateway.Api.Errors;

public static class ProblemDetailsTraceId
{
    public static void Apply(ProblemDetailsContext context)
    {
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;
    }
}
```

Criar `src/Gateway/Climato.Gateway.Api/Observability/RequestLogLevel.cs`:

```csharp
using Serilog.Events;

namespace Climato.Gateway.Api.Observability;

public static class RequestLogLevel
{
    public static LogEventLevel Get(HttpContext context, double elapsedMilliseconds, Exception? exception)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Verbose;
        }

        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return LogEventLevel.Information;
    }
}
```

Criar `src/Gateway/Climato.Gateway.Api/Observability/CorrelationIdMiddleware.cs`:

```csharp
using System.Diagnostics;

namespace Climato.Gateway.Api.Observability;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;

        if (activity is not null)
        {
            var traceId = activity.TraceId.ToHexString();
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = traceId;
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}
```

- [ ] **Passo 5: confirmar que passa.**

Run: `dotnet test --project tests/Climato.Gateway.UnitTests`
Expected: todos verdes (os anteriores + 10 novos).

- [ ] **Passo 6:** `/revisar-padrao` e commit.

```bash
git add src/Gateway/Climato.Gateway.Api tests/Climato.Gateway.UnitTests
git commit -m "Adiciona o correlation ID, o traceId puro no ProblemDetails e o nível do request log no Gateway"
```

---

### Tarefa 4: Gateway: ligar Serilog, OpenTelemetry, CORS e pipeline

**Interfaces consumidas:** as três da Tarefa 3.
**Interfaces produzidas:** `Climato.Gateway.Api.Observability.ObservabilityExtensions.AddObservability(this WebApplicationBuilder builder)`.

- [ ] **Passo 1:** criar `src/Gateway/Climato.Gateway.Api/Observability/ObservabilityExtensions.cs`. Conferir no pacote `Npgsql.OpenTelemetry` o namespace da extensão `AddNpgsql()` (esperado: `OpenTelemetry.Trace`, nesse caso não precisa de `using` extra; se for `Npgsql`, adicionar `using Npgsql;`).

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace Climato.Gateway.Api.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "Climato.Gateway";
    private const string DevelopmentTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", ServiceName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName);

            if (environment.IsDevelopment())
            {
                logger.WriteTo.Console(outputTemplate: DevelopmentTemplate);
            }
            else
            {
                logger.WriteTo.Console(new CompactJsonFormatter());
            }
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    tracing.AddOtlpExporter();
                }
            });

        return builder;
    }
}
```

- [ ] **Passo 2:** em `src/Gateway/Climato.Gateway.Api/DependencyInjection.cs`:
  - Trocar `services.AddProblemDetails();` por `services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsTraceId.Apply);`.
  - Na política `Web` do CORS, incluir o header exposto e o `using Climato.Gateway.Api.Observability;`:

```csharp
            options.AddPolicy("Web", policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders(CorrelationIdMiddleware.HeaderName));
```

- [ ] **Passo 3:** em `src/Gateway/Climato.Gateway.Api/Program.cs`, adicionar `using Climato.Gateway.Api.Observability;` e `using Serilog;`, chamar `builder.AddObservability();` antes do `builder.Services.AddGatewayApi(...)` e abrir o pipeline assim:

```csharp
app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel.Get);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Web");
app.UseAuthentication();
app.UseAuthorization();
```

O bloco de migrations e o `MapOpenApi`/`MapScalarApiReference` continuam onde estão.

- [ ] **Passo 4:** em `src/Gateway/Climato.Gateway.Api/appsettings.json`, trocar a seção `Logging` por:

```json
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    }
  },
```

- [ ] **Passo 5: build e testes.**

Run: `dotnet build Climato.slnx` → 0 erros, 0 avisos novos.
Run: `dotnet test --solution Climato.slnx` → todos verdes.

A verificação manual do Gateway fica na Tarefa 5, porque ele precisa de Postgres.

- [ ] **Passo 6:** `/revisar-padrao` e commit.

```bash
git add src/Gateway/Climato.Gateway.Api
git commit -m "Adiciona Serilog, OpenTelemetry e o header X-Correlation-ID no Gateway"
```

---

### Tarefa 5: Compose com Jaeger e verificação de ponta a ponta

- [ ] **Passo 1:** em `docker-compose.yml`:
  - Nos serviços `provider` e `gateway`, incluir em `environment`:

```yaml
      OTEL_EXPORTER_OTLP_ENDPOINT: ${OTEL_EXPORTER_OTLP_ENDPOINT:-}
```

  - Novo serviço, entre `redis` e `provider`:

```yaml
  jaeger:
    image: jaegertracing/jaeger:2.22.0
    profiles: [observability]
    ports:
      - "16686:16686"
```

- [ ] **Passo 2:** no fim do `.env.example`:

```bash

# Traces no Jaeger (docker compose --profile observability up). Vazio = sem exportar.
# OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317
```

- [ ] **Passo 3: validar o compose.**

Run: `docker compose config --quiet` → sem erro. Se faltar o `.env`, exportar as variáveis com valores fictícios só na sessão do terminal, como no job `docker` do CI.
Run: `docker compose --profile observability config --services` → inclui `jaeger`.

- [ ] **Passo 4: subir sem o web (porta 8080 ocupada).** Usar o `.env` existente sem abri-lo. Se não houver `.env`, perguntar ao usuário. Exportar `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317` só na sessão do terminal:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317 docker compose --profile observability up -d --build postgres redis jaeger provider gateway
```

- [ ] **Passo 5: correlation ID e traces.**
  - Registrar um usuário de teste e fazer login (`POST http://localhost:5001/auth/register` e `/auth/login`, com `email`, `name` e `password`), guardando o token só na variável do terminal.
  - `curl -s -i -H "Authorization: Bearer $TOKEN" http://localhost:5001/weathers/Londres`: a resposta tem `X-Correlation-ID` com 32 hex.
  - Abrir `http://localhost:16686`, buscar o trace pelo ID: spans `Climato.Gateway` (ASP.NET, Npgsql, HttpClient) → `Climato.Provider` (ASP.NET, HttpClient para OpenWeather/Pexels). A URL da OpenWeather aparece sem o valor de `appid` (query redigida).
  - `curl -s -i -X OPTIONS -H "Origin: http://localhost:8080" -H "Access-Control-Request-Method: GET" http://localhost:5001/cities`: o preflight responde. Com um `GET` com `Origin`, a resposta traz `Access-Control-Expose-Headers: X-Correlation-ID`.

- [ ] **Passo 6: logs JSON e log forging.**
  - `docker compose stop redis`, depois `curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5001/weathers/Lon%0Adon`. Com o Redis fora, o `ProviderCache` loga `Warning` com o texto da cidade.
  - `docker compose logs provider --tail 20`: cada evento é **uma** linha JSON, o `Text` aparece com `\n` escapado, e `@tr` é o mesmo TraceId do `X-Correlation-ID` dessa resposta.
  - `docker compose logs gateway --tail 20`: linhas JSON com `Service: "Climato.Gateway"`, a linha do request log e nenhum log de `/health`.
  - `docker compose start redis`.

- [ ] **Passo 7: 500/503 com o mesmo ID.** `docker compose stop provider`, depois `curl -s -i -H "Authorization: Bearer $TOKEN" http://localhost:5001/weathers/Londres`: 503 `ProblemDetails`, com `traceId` igual ao header `X-Correlation-ID` (o header sobrevive ao `Response.Clear()` do exception handler). Em seguida, `docker compose start provider`.

- [ ] **Passo 8:** `docker compose --profile observability down` (sem `-v`, para não apagar o banco local do usuário). Não tocar nos containers do `statement-importer`.

- [ ] **Passo 9:** `/revisar-padrao` e commit.

```bash
git add docker-compose.yml .env.example
git commit -m "Adiciona o Jaeger no compose com o profile observability"
```

---

### Tarefa 6: Documentação e fechamento da fase

- [ ] **Passo 1: `docs/architecture/backend.md`**
  - Stack: a linha `Logs` passa a "Serilog (console; JSON compacto fora de Development) + OpenTelemetry (traces, OTLP opcional)", com alvo `—`.
  - Estrutura da Solução: incluir `Observability` na lista de pastas das duas Apis.
  - Testes: o Provider.UnitTests cobre também a Api (`Errors`, `Observability`).
  - Nova seção `### Observabilidade`, depois de `### Health checks`:
    - Serilog via `AddObservability` em cada Api. Nível pela seção `Serilog:MinimumLevel`. Console em texto em Development e `CompactJsonFormatter` fora dele. Propriedades `Service` e `Environment`.
    - Log de entrada do usuário **sempre por placeholder** (`{CityName}`), nunca por interpolação `$"..."` ou concatenação: o JSON escapa os valores e fecha o log forging.
    - Correlation ID = TraceId W3C. `traceparent` propagado pelo `HttpClient`. `X-Correlation-ID` na resposta do Gateway (via `OnStarting`, exposto no CORS). `traceId` puro no `ProblemDetails` (`ProblemDetailsTraceId`).
    - OTel: ASP.NET Core, HttpClient e Npgsql (Gateway). `/health` fora dos traces e do request log (`RequestLogLevel`). Exportador OTLP só com `OTEL_EXPORTER_OTLP_ENDPOINT`.
    - Redação de URI do `HttpClient` mantida (chave em `appid=`).
  - Autenticação: o pipeline passa a ser `UseSerilogRequestLogging` → `CorrelationIdMiddleware` → `UseExceptionHandler` → `UseStatusCodePages` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization`.
- [ ] **Passo 2: `docs/architecture/review-checklist.md`**, novas linhas no Backend:

```markdown
| BE-29 | 🟡 | 06 | Log com interpolação | `logger.Log*($"...")` ou concatenação com dado da requisição em vez de placeholder `{Nome}` | [Observabilidade](backend.md#observabilidade) |
| BE-30 | 🟡 | 06 | Log fora do `ILogger<T>` | `Console.WriteLine`, `Debug.WriteLine` ou `Log.Logger` estático em código de aplicação | [Observabilidade](backend.md#observabilidade) |
```

- [ ] **Passo 3: `docs/architecture/system-design.md`:** nova subseção `### Observabilidade` em Fluxos, com o TraceId atravessando web → Gateway (`X-Correlation-ID`) → Provider (`traceparent`) → OpenWeather/Pexels, e o Jaeger opcional no compose.
- [ ] **Passo 4: `docs/runbook.md`**
  - Seção Docker: tabela com `Jaeger | http://localhost:16686 (só com --profile observability)` e um parágrafo "Logs e traces": `docker compose logs -f gateway provider` (JSON, uma linha por evento, `@tr` = TraceId), o header `X-Correlation-ID` para achar a requisição, e como ligar o Jaeger (`OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317` no `.env` + `docker compose --profile observability up`).
  - Seção `dotnet run`: o console sai em texto com o TraceId. O nível agora vem de `Serilog:MinimumLevel`, e a seção `Logging` de um `appsettings.Development.json` local deixa de ter efeito.
- [ ] **Passo 5: `docs/features/00-roteiro-portfolio/auditoria-inicial.md`:** A25 com status `✅ CI, Docker e health checks (Fases 03 e 05) · logs estruturados (Fase 06)`.
- [ ] **Passo 6: `docs/project-overview.md`**
  - Remover a pendência `[Fase 06] Log forging...`.
  - Marcar a Fase 06 como `✅ Concluída (AAAA-MM-DD)` com a data do commit final, no formato das fases anteriores.
  - Registrar as pendências encontradas na execução, se houver.
- [ ] **Passo 7: verificação final** (`verification-before-completion`):

```bash
dotnet build Climato.slnx
dotnet test --solution Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

Expected: build sem erros nem avisos novos; todos os testes verdes (73 + 18 novos = 91); lint e build do web sem erro.

- [ ] **Passo 8:** marcar os checkboxes deste plano, `/revisar-padrao` sem 🔴 e commit.

```bash
git add docs
git commit -m "Documenta a observabilidade e fecha a Fase 06"
```

- [ ] **Passo 9:** `/requesting-code-review` → `/finishing-a-development-branch` → PR `dev/observabilidade` → `master`, citando A25 e a pendência de log forging. O PR só é mergeado com CI ✅.
