# Design — Fase 06: Observabilidade

**Data:** 2026-11-04
**Fase:** 06
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#06--observabilidade) · [`backend.md`](../../architecture/backend.md) · [auditoria](../00-roteiro-portfolio/auditoria-inicial.md) (A25 logs estruturados) · pendência `[Fase 06]` de log forging no [`project-overview.md`](../../project-overview.md#pendências-futuras)
**Branch:** `dev/observabilidade`

---

## Objetivo

Uma requisição do web pode ser seguida de ponta a ponta: o Gateway devolve um ID, e esse mesmo ID aparece nos logs JSON dos dois serviços, no `ProblemDetails` de erro e no trace do Jaeger (Gateway → Provider → OpenWeather/Pexels, Gateway → Postgres). Os logs estruturados fecham o A25, e o JSON resolve o log forging de `cityName` e `query` no Provider.

**Fora do escopo:**
- Métricas OTel (`AddMeter`, Prometheus) e logs via OTLP: o console JSON basta para portfólio.
- Instrumentação do Redis (`OpenTelemetry.Instrumentation.StackExchangeRedis` é beta).
- Sinks externos (Seq, Loki, Elastic).
- Desligar a redação de URI do `HttpClient`: a chave da OpenWeather vai em `appid=`.

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Escopo | Serilog + correlation ID + **OpenTelemetry com Jaeger** | Só Serilog; OTel sem exportador |
| Correlation ID | **TraceId W3C** do `Activity`: o `HttpClient` já propaga `traceparent` e o Gateway devolve o TraceId em `X-Correlation-ID` | Header `X-Correlation-ID` próprio entre os serviços; os dois juntos |
| Formato do console | **JSON compacto fora de Development**, texto em Development (escolha no código, porque o `appsettings.Development.json` não é versionado) | JSON sempre; texto + sanitização de CR/LF |
| Jaeger | **`profile: observability`** no compose; o exportador só liga com `OTEL_EXPORTER_OTLP_ENDPOINT` | Jaeger sempre ligado |
| Onde mora o código | **Duplicado em cada `*.Api`** (`AddObservability`), sem projeto compartilhado | `Climato.ServiceDefaults` compartilhado (acopla os serviços e pediria ADR) |
| Spans de SQL | **`Npgsql.OpenTelemetry`** (estável) | `OpenTelemetry.Instrumentation.EntityFrameworkCore` (beta) |

---

## 1. Serilog

Pacote `Serilog.AspNetCore`, que já traz `Sinks.Console`, `Formatting.Compact` e `Settings.Configuration`. Fica nas duas Apis.

- Extensão `AddObservability(this WebApplicationBuilder builder)` em `Api/Observability/ObservabilityExtensions.cs` de cada serviço, chamada no `Program.cs` antes de `Build()`.
- `builder.Services.AddSerilog((services, logger) => ...)`:
  - `ReadFrom.Configuration(configuration)`: o nível mínimo vem da seção `Serilog` do `appsettings.json`, que **substitui** a seção `Logging` (`Default: Information`, `Microsoft.AspNetCore: Warning`).
  - `ReadFrom.Services(services)`, `Enrich.FromLogContext()`, `Enrich.WithProperty("Service", "Climato.Gateway" | "Climato.Provider")` e `Enrich.WithProperty("Environment", environment.EnvironmentName)`.
  - Console: em Development, template de texto `[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}`; fora dele, `new CompactJsonFormatter()`. O Serilog 4 captura o `Activity` atual, então `@tr` (TraceId) e `@sp` (SpanId) saem no JSON sem enricher.
- `app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel.Get)` como **primeiro** middleware, antes do `UseExceptionHandler`. Gera uma linha por requisição com método, rota, status e tempo, e vê o status final (o 500 já tratado), sem repetir a stack trace que o `GlobalExceptionHandler` loga. A classe estática `RequestLogLevel` fica em `Api/Observability/` de cada serviço e decide o nível:
  - `/health/*` → `Verbose`, filtrado pelo nível mínimo. O polling da hospedagem não polui o log, e check falho já é logado pelo próprio `HealthCheckService`.
  - Exceção ou status ≥ 500 → `Error`.
  - O resto → `Information`.
- Overrides de nível: `Microsoft.AspNetCore`, `System.Net.Http.HttpClient` e (no Gateway) `Microsoft.EntityFrameworkCore` em `Warning`. A linha de request e os spans substituem esses logs por chamada.
- **Log forging:** os placeholders atuais (`{CityName}`, `{Text}`, `{Path}`) continuam como estão. No `CompactJsonFormatter` os valores string são escapados (`\n` → `\\n`), então uma entrada não consegue forjar uma linha nova de log. Em Development o texto pode quebrar a linha, mas lá a entrada é de quem roda a máquina.
- Os logs atuais com `ILogger<T>` não mudam: o Serilog entra por baixo da abstração. A Application não ganha dependência nova.

## 2. Correlation ID

O ID é o **TraceId W3C** (32 hex) do `Activity` da requisição.

- **Gateway → Provider:** o `HttpClient` do .NET já injeta `traceparent` nas chamadas, e o ASP.NET Core do Provider continua o mesmo trace. Não há header nem handler próprio.
- **Resposta ao web:** `CorrelationIdMiddleware`, só no Gateway (`Api/Observability/`), lê o TraceId de `Activity.Current` na entrada e registra um `Response.OnStarting` que grava `X-Correlation-ID`. Usa `OnStarting`, e não gravação direta, porque o `ExceptionHandlerMiddleware` faz `Response.Clear()` (apaga os headers) antes de escrever o 500, mas preserva os callbacks. Entra no pipeline logo depois do request logging e antes do `UseExceptionHandler`. O `Activity` sempre existe porque a instrumentação ASP.NET do OTel está sempre registrada. Como o tipo é anulável, sem `Activity` o header simplesmente não é escrito.
- **CORS:** a política `Web` ganha `.WithExposedHeaders("X-Correlation-ID")`, para que o front consiga ler o header. O web não muda nesta fase.
- **ProblemDetails:** o `AddProblemDetails` padrão grava `traceId` como `Activity.Id` (`00-<trace>-<span>-01`). Nas duas Apis, `AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsTraceId.Apply)` troca esse valor pelo TraceId puro, com fallback para `HttpContext.TraceIdentifier`. O ID de um 500 passa a ser o mesmo do header, do log e do Jaeger. A classe estática `ProblemDetailsTraceId` fica em `Api/Errors/` de cada serviço.

## 3. OpenTelemetry (traces)

Pacotes: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http` e `OpenTelemetry.Exporter.OpenTelemetryProtocol` nas duas Apis; `Npgsql.OpenTelemetry` só no Gateway.

Tudo dentro do mesmo `AddObservability`:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Climato.Gateway"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
            .AddHttpClientInstrumentation()
            .AddNpgsql(); // só no Gateway

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            tracing.AddOtlpExporter();
        }
    });
```

- Sem `OTEL_EXPORTER_OTLP_ENDPOINT` (dev local, CI, testes), o trace existe só para dar TraceId aos logs e ao header, sem exportar nada e sem erro de conexão.
- O `AddOtlpExporter()` lê endpoint e protocolo das variáveis padrão `OTEL_EXPORTER_OTLP_*`, sem Options próprias.
- Os spans do `HttpClient` levam a URL já redigida pelo .NET (query string oculta por padrão), então a chave `appid=` não vaza para o Jaeger. Isso é conferido na verificação manual.

## 4. Compose

```yaml
  jaeger:
    image: jaegertracing/jaeger:2.22.0   # Jaeger v2, all-in-one por padrão (UI 16686, OTLP gRPC 4317)
    profiles: [observability]
    ports:
      - "16686:16686"
```

- `provider` e `gateway` ganham `OTEL_EXPORTER_OTLP_ENDPOINT: ${OTEL_EXPORTER_OTLP_ENDPOINT:-}`.
- O `.env.example` documenta `OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317`, comentado, para usar com `docker compose --profile observability up`.
- A porta OTLP (4317) não é publicada no host, porque só os containers falam com ela.
- O CI não muda: o job de imagens faz só `docker compose build`.

## 5. Testes (TDD)

| Teste | Projeto | Cenário |
|---|---|---|
| `CorrelationIdMiddlewareTests` | `Climato.Gateway.UnitTests/Api/Observability` | Com `Activity` iniciado, o header `X-Correlation-ID` igual ao `TraceId` é gravado no início da resposta; o `next` é chamado. |
| `RequestLogLevelTests` | `Climato.{Gateway,Provider}.UnitTests/Api/Observability` | `/health/ready` com 503 → `Verbose`; exceção → `Error`; 500 → `Error`; 200 e 404 → `Information`. |
| `ProblemDetailsTraceIdTests` | `Climato.Gateway.UnitTests/Api/Errors` | Com `Activity`, o `traceId` é o TraceId puro; sem `Activity`, é o `TraceIdentifier`. |
| `ProblemDetailsTraceIdTests` | `Climato.Provider.UnitTests/Api/Errors` | Os mesmos dois cenários. O projeto passa a referenciar `Climato.Provider.Api`. |

Não entram em teste unitário a configuração do Serilog e do OTel (é só ligação de biblioteca) nem o escape do JSON (comportamento do `CompactJsonFormatter`). As duas coisas são conferidas à mão:
- `docker compose up`, buscar uma cidade com `%0A` no nome e ver em `docker compose logs provider` uma linha JSON só, com `\n` escapado e `@tr` presente.
- `docker compose --profile observability up`: abrir uma cidade no web e ver no Jaeger um trace com Gateway → Provider → OpenWeather/Pexels e os spans do Postgres; o TraceId bate com o `X-Correlation-ID`; a URL da OpenWeather aparece sem `appid`.

## 6. Documentação

- `backend.md`: pipeline passa a começar por `UseSerilogRequestLogging` → `CorrelationIdMiddleware` (Gateway) → `UseExceptionHandler`; a linha "Logs" da stack sai do alvo; nova seção **Observabilidade** (Serilog, formato por ambiente, TraceId como correlation ID, OTel com exportador condicional, `/health` fora dos logs e traces, logar entrada do usuário só por placeholder, nunca por interpolação).
- `review-checklist.md`: BE-29 (log com interpolação `$"..."` ou concatenação em vez de placeholder) e BE-30 (`Console.WriteLine` ou logger fora do `ILogger<T>`).
- `runbook.md`: profile `observability`, Jaeger em `localhost:16686`, como ler o JSON (`docker compose logs`) e o header `X-Correlation-ID`.
- `system-design.md`: correlação via `traceparent` entre os serviços.
- `auditoria-inicial.md`: A25 ✅. `project-overview.md`: tirar a pendência `[Fase 06]` de log forging e marcar a fase ✅ no commit final.
