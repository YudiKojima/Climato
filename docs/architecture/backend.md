# Backend — Arquitetura e Padrões

Vale para `src/Gateway/` e `src/Provider/`. Seções marcadas **(a partir da Fase NN)** descrevem padrões já decididos que ainda não estão no código; ao implementá-los, remover a marcação.

## Stack

| | Atual | Alvo |
|---|---|---|
| Runtime | .NET 10 ([ADR 0004](decisions/0004-dotnet-10.md)) | — |
| API | ASP.NET Core com controllers; OpenAPI nativo + Scalar | — |
| Persistência (Gateway) | EF Core 10 + Npgsql via `AddDbContext`, Migrations | — |
| Mapeamento | Mapster, só no Gateway ([ADR 0005](decisions/0005-mapster.md)) | — |
| JSON externo | System.Text.Json + DTOs tipados por API externa (`ReadFromJsonAsync`) | — |
| Validação | FluentValidation + `ValidationFilter` | — |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) + `PasswordHasher<User>` | — |
| Resiliência / cache | `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`) / `IDistributedCache` (Redis opcional, memória sem connection string) no Provider | — |
| Health checks | `/health/live` e `/health/ready` (`Microsoft.Extensions.Diagnostics.HealthChecks`) | — |
| Testes | xUnit v3 (Microsoft.Testing.Platform), Shouldly, NSubstitute | Testcontainers, WireMock.Net (integração, backlog) |
| Logs e traces | Serilog (console; JSON compacto fora de Development) + OpenTelemetry (traces; exportador OTLP opcional) | — |

## Estrutura da Solução

Cada serviço segue camadas no estilo Clean Architecture:

```
Climato.slnx                         ← solução única; Directory.Build.props / Directory.Packages.props / global.json na raiz
src/Gateway/
├── Climato.Gateway.Api/             ← Program.cs, DependencyInjection.cs, Controllers, Filters, Errors, Extensions, Observability, OpenApi
├── Climato.Gateway.Application/     ← Contracts, Services, Model (DTOs: Auth, Cities, Users, Weathers), Mappings (Mapster), Validators, Options, Exceptions, DependencyInjection.cs
├── Climato.Gateway.Domain/          ← Entidades e interfaces de repositório por agregado (Cities/, Users/), Exceptions/
└── Climato.Gateway.Infrastructure/  ← Data (ClimatoDbContext, Configurations, Migrations, Repositories), Http (clients, ProviderHealthCheck), Auth (JwtTokenService), DependencyInjection.cs
src/Provider/
├── Climato.Provider.Api/            ← Program.cs, DependencyInjection.cs, Controllers, Errors, Observability
├── Climato.Provider.Application/    ← OpenWeatherClient, AppServices (clima, previsão, busca, imagem), `CityResolver` e cache (Services), Contracts, Model (DTOs da OpenWeather/Pexels), Exceptions, Options, DependencyInjection.cs
└── Climato.Provider.Domain/         ← Weathers (Weather, Forecast — contratos devolvidos ao Gateway), Locations
```

```
tests/
├── Climato.Provider.UnitTests/  ← AppServices e cache do Provider; Api (Errors, Observability)
└── Climato.Gateway.UnitTests/   ← domínio, AppServices, validators; Api (GlobalExceptionHandler, Errors, Observability)
```

### Dependências entre camadas

```
API ──▶ Application ──▶ Domain
 └────▶ Infrastructure ──▶ Domain
```

- **Domain** não referencia nada do projeto nem pacotes de infraestrutura.
- **Application** não referencia Infrastructure; usa as interfaces do Domain.
- **API** só referencia Infrastructure para registrar o DI.
- Pacotes ficam no projeto que os usa (EF Core/Npgsql só na Infrastructure).
- Versões de pacote só em `Directory.Packages.props`; `.csproj` sem `Version`.

## Convenções de Código

### Geral
- `Nullable` habilitado; nenhum aviso de nulabilidade novo.
- Nomes de tipos e membros em inglês (padrão .NET); **mensagens ao usuário, exceções e docs em português**.
- Namespace igual ao caminho da pasta, no formato file-scoped (`namespace X;`) em arquivos novos ou tocados.
- Campos privados `camelCase` sem `_`, como no código existente.
- Chaves em todo `if`/`else`/`foreach`.
- Async de ponta a ponta: métodos retornam `Task`/`Task<T>`, nunca `async void`; sem `.Result`/`.Wait()`. `CancellationToken` obrigatório (sem default) nas interfaces e propagado do controller ao repositório e ao `HttpClient`.

### Controllers
- Finos: validam a entrada (via `ValidationFilter`, que roda o `IValidator<T>` do argumento), chamam um AppService e traduzem o resultado em status HTTP.
- `ActionResult<T>` com o DTO de resposta; XML doc (`/// <summary>`) em português em cada action — aparece na documentação OpenAPI/Scalar.
- Rotas no plural e minúsculas (`/cities`, `/weathers`, `/forecasts`, `/locations`, `/auth`).
- `[Authorize]` em todo controller; `[AllowAnonymous]` explícito só em registro, login, demonstração e health checks. O usuário atual vem das claims, nunca de parâmetro.
- `[EnableRateLimiting]` em todo controller: `RateLimitPolicies.Auth` no `AuthController`, `RateLimitPolicies.Api` nos demais (ver [Rate limiting](#rate-limiting-gateway)).
- `GET` só lê: nada de gravar no banco numa consulta. Efeito colateral vai num `POST`/`PATCH`/`DELETE` explícito (ex.: o histórico é registrado por `POST /cities`, não pelo `GET /weathers`).
- Usuário atual: `User.GetUserId()` (`Api/Extensions/ClaimsPrincipalExtensions`), que lê a claim `sub`.

### DTOs (Application)
- Ficam em `Application/Model/<Agregado>/`; sufixos `Dto` (resposta), `PostDto`/`PutDto` (entrada pelo corpo), `Query` (entrada pela query string, recebida com `[FromQuery]`, ex.: `LocationSearchQuery`). Propriedades de `Query` são anuláveis: com `string` não anulável o `[ApiController]` aplica `[Required]` implícito e responde em inglês antes do validator. O parâmetro da action não pode ter o nome de uma chave da query string (`[FromQuery] LocationSearchQuery request`, não `query`): o binder usaria o nome como prefixo e procuraria `query.Query`.
- DTO de resposta **nunca** expõe senha, hash ou dado interno.
- Entidades do Domain nunca saem pelo controller.
- Todo DTO de entrada tem um `AbstractValidator<T>` em `Application/Validators/`, com mensagens em português.

### Mapeamento (Mapster)
- Configuração em `Application/Mappings/` com uma classe `IRegister` por agregado (`CityMappings`, `UserMappings`).
- AppServices recebem `MapsterMapper.IMapper` e chamam `mapper.Map<TDestino>(origem)`. Nada de `.Adapt()` ou `TypeAdapterConfig` fora de `Mappings/` e do `DependencyInjection.cs`.
- Decisão: [ADR 0005](decisions/0005-mapster.md).

### Entidades (Domain)
- Construtor com os campos obrigatórios gera `Id` e `CreatedAt`; construtor vazio só para o EF; setters privados quando a entidade tem método de mudança (ex.: `User.SetPasswordHash`).
- Regra que muda estado da entidade é método dela (ex.: `city.ToggleFavorite()`), não lógica solta no AppService.
- Violação de regra de negócio lança `DomainException` (vira 409).

### Repositórios (Infrastructure)
- Interface no Domain, implementação na Infrastructure.
- Recebem o `ClimatoDbContext` pelo construtor e são registrados como `Scoped`. Todo método recebe `CancellationToken`.
- Consultas filtradas por usuário recebem `userId` explícito; nunca retornar dados de outro usuário.

### Comunicação HTTP
- **Typed HttpClient** registrado com `AddHttpClient<IContrato, Implementacao>()`, sem `AddTransient` duplicado (A20).
- `BaseAddress` e chaves vêm de configuração (`Options`), nunca literais no código.
- Parâmetros de query sempre com `Uri.EscapeDataString`.
- DTOs tipados por API externa (`OpenWeatherCurrentResponse`, `PexelsSearchResponse`) em `Application/Model/<Api>/`, lidos com `ReadFromJsonAsync<T>`. Nada de `Dictionary<string, object>`, `JObject` ou Newtonsoft.
- `using` em todo `HttpResponseMessage` (e `HttpRequestMessage`, quando criado à mão). Headers fixos (ex.: `Authorization` do Pexels) vão em `DefaultRequestHeaders` no registro do client.
- Resiliência com `AddStandardResilienceHandler()` em todo typed client. Timeouts escalonados: a tentativa de quem chama é maior que a soma dos timeouts totais das dependências que o chamado consulta em sequência (Gateway → Provider 25 s/30 s > OpenWeather 2 × 9 s (geocoding + clima) + Pexels 5 s + folga do cache; Provider → OpenWeather 4 s/9 s; Provider → Pexels 2 s/5 s). `CircuitBreaker.SamplingDuration` ≥ 2× o timeout por tentativa. O Gateway só re-tenta falha de transporte (`HttpRequestException` ou timeout de conexão, `TaskCanceledException` com `TimeoutException` interna), uma vez, depois de 500 ms; 5xx do Provider e resposta lenta (`TimeoutRejectedException`) não são re-tentados.
- Conexão com o Provider limitada a 2 s (`SocketsHttpHandler.ConnectTimeout`, no client tipado e no do health check): sem isso um host que não responde segura cada tentativa ~21 s. Provider fora → 503 em ≤ 5 s (2 s + 0,5 s + 2 s).
- Client traduz a resposta: 404 → `null`; outro status de erro, `HttpRequestException`, `ExecutionRejectedException` (Polly), `JsonException` ou cancelamento que não veio de quem chamou (timeout de conexão) → exceção tipada de indisponibilidade (`ExternalServiceUnavailableException` no Provider, `ProviderUnavailableException` no Gateway).
- Provider: toda chamada à OpenWeather passa pelo typed client `IOpenWeatherClient` (`OpenWeatherClient`), que concentra a tradução de erros num `GetAsync<T>` privado; AppServices não recebem `HttpClient` da OpenWeather. No Gateway, o `WeatherProviderClient` faz o mesmo para o Provider.
- Clima e previsão resolvem a cidade antes (`ICityResolver`/`CityResolver`): primeiro resultado do `geo/1.0/direct`, nome em português (`local_names.pt`, senão o original) e coordenadas; a consulta à OpenWeather é por `lat`/`lon` (formatados com `CultureInfo.InvariantCulture`). `City`/`Country` da resposta vêm do resolvido; a imagem da Pexels usa o nome original.

### Erros
- Respostas de erro em `ProblemDetails` (`AddProblemDetails` + `UseStatusCodePages` + `GlobalExceptionHandler`). `DomainException` → 409 com a mensagem; validação → 400 `ValidationProblemDetails`; resto → 500 "Erro interno." sem detalhes. Erros esperados no controller com `Problem(statusCode, title)` ou `NotFound()`.
- Não usar exceção para fluxo esperado (cidade não encontrada → retorno `null`/resultado, controller devolve 404).
- Dependência fora (API externa, Provider) → exceção tipada de indisponibilidade → **503** no `GlobalExceptionHandler`, log `Warning`. Cidade inexistente continua sendo fluxo esperado (`null` → 404).
- Cliente abortou (`OperationCanceledException` com `RequestAborted` cancelado) → 499 sem corpo, log `Information`.
- Violação de constraint que reflete regra (e-mail único) → o repositório traduz para `DomainException` (409); FK de usuário inexistente → `UserNotFoundException` → 401.
- 400 automático do `[ApiController]` usa o mesmo `ValidationProblemDetails` do `ValidationFilter` (`InvalidModelStateResponseFactory`, título "Dados inválidos."). No Gateway o `ModelState` passa por `InvalidRequest.Sanitize`: erro de leitura do corpo (JSON malformado, tipo errado, corpo vazio) vira só `body: ["JSON inválido."]`, e valor inválido de rota/query vira "Valor inválido." na mesma chave; `AllowInputFormatterExceptionMessages = false` como segunda barreira. Nenhuma mensagem do System.Text.Json nem nome de tipo .NET sai na resposta.
- Limite excedido → 429 com `Retry-After` e `ProblemDetails` "Muitas requisições. Tente novamente em instantes." (`RateLimitPolicies.OnRejectedAsync`).
- Falha opcional (imagem do Pexels, cache) não derruba a resposta: log `Warning` e segue.

### Cache (Provider)
- `IDistributedCache` atrás de `IProviderCache` (`Services/ProviderCache`), genérico: `GetAsync<T>(prefixo, texto)` / `SetAsync<T>(prefixo, texto, valor, duração)`. Redis se `ConnectionStrings:Redis` estiver preenchida; senão `AddDistributedMemoryCache`.
- Chave `{prefixo}:{texto-normalizado}` (trim, espaços colapsados, minúsculas, sem acentos); valor em JSON; TTL absoluto por uso: `clima` 10 min (`clima-sem-imagem`, 10 min, para `?image=false` dos cards, que não chama a Pexels), `previsao` 30 min, `locais` 24 h (lista vazia também é cacheada), `cidades` 24 h (só cidade encontrada; vazio não é cacheado).
- Só resposta de sucesso entra no cache (404 e falhas não). Erro do cache → `Warning` e segue sem cache.

### Health checks
- `/health/live`: sem checks (processo vivo). `/health/ready`: checks com a tag `ready`.
- Gateway: Postgres (`AddDbContextCheck`) e Provider (`ProviderHealthCheck` → `GET health/ready`, client nomeado sem resiliência, timeout de 4 s e conexão de 2 s). Provider: Redis, só se configurado, com `failureStatus: HealthStatus.Degraded` (200; Redis fora não derruba o `ready` do Gateway).
- APIs externas (OpenWeather, Pexels) **não** entram: gastariam cota.
- No Gateway, `MapHealthChecks(...).AllowAnonymous()`.
- Timeout de 3 s por check (`PostConfigure<HealthCheckServiceOptions>`), para um check travado não segurar o `/health/ready`. Exceção: o check `provider` do Gateway tem 5 s, mais que os 3 s do check de Redis do Provider, para um Redis travado chegar ao Gateway como `Degraded` (200) e não como timeout.

### Observabilidade
- **Serilog** via `builder.AddObservability()` (`Api/Observability/ObservabilityExtensions`, duplicada de propósito em cada Api). Nível em `Serilog:MinimumLevel` no `appsettings.json` (a seção `Logging` não é usada). Console em texto com `{TraceId}` em Development e `CompactJsonFormatter` fora dele; propriedades `Service` e `Environment` em todo evento. Código de aplicação loga só por `ILogger<T>`.
- Entrada do usuário entra no log **sempre por placeholder** (`{CityName}`), nunca por interpolação `$"..."` ou concatenação: o JSON escapa o valor (`\n` não forja linha) e a propriedade fica pesquisável.
- `UseSerilogRequestLogging` é o primeiro middleware: uma linha por requisição com o status final, sem repetir a exceção que o `GlobalExceptionHandler` já loga. `RequestLogLevel` decide o nível: `/health/*` → `Verbose` (fora do log), exceção ou ≥ 500 → `Error`, resto → `Information`.
- **Correlation ID = TraceId W3C.** O `HttpClient` propaga `traceparent` ao Provider. O Gateway é borda pública e **ignora** `traceparent`/`baggage` de entrada: `IncomingTraceContextBlocker` registrado como `DistributedContextPropagator` (o hosting sempre abre trace raiz) e `Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator())` (a instrumentação do OTel não reextrai o header). O cliente não escolhe o TraceId nem desliga a amostragem. O Provider continua aceitando o `traceparent` do Gateway. O Gateway devolve o TraceId em `X-Correlation-ID` (`CorrelationIdMiddleware`, via `Response.OnStarting` para sobreviver ao `Response.Clear()` do exception handler; exposto no CORS). O `traceId` do `ProblemDetails` é o TraceId puro (`ProblemDetailsTraceId` em `CustomizeProblemDetails`).
- **OpenTelemetry** (traces): ASP.NET Core (sem `/health`), HttpClient e, no Gateway, Npgsql (`Npgsql.OpenTelemetry`). O exportador OTLP só é registrado com `OTEL_EXPORTER_OTLP_ENDPOINT` definido; sem ele o trace só alimenta o TraceId dos logs e do header.
- A redação de URI do `HttpClient` e do OTel fica ligada (a query aparece como `?*`): a chave da OpenWeather vai em `appid=`.

### Rate limiting (Gateway)
- Rate limiter nativo do ASP.NET Core (`AddGatewayRateLimiting`, `Api/RateLimiting/`), `app.UseRateLimiter()` depois do `UseAuthorization` (a política `api` precisa das claims).
- Janela fixa, sem fila: política `auth` por IP (`AuthPermitLimit`, padrão 10/min) e política `api` por usuário (claim `sub`; usuário de demonstração, `sub` + IP, para um visitante não esgotar o limite dos outros; sem usuário, IP — `ApiPermitLimit`, padrão 120/min, folga para a Home, que faz uma chamada por card). Limites em `RateLimiting:*` (`RateLimitingOptions`, validadas no startup).
- Todo controller tem `[EnableRateLimiting]`; health checks ficam fora.
- O IP é o `RemoteIpAddress`: atrás do proxy da hospedagem é preciso `ForwardedHeaders` (Fase 08b), senão todos os visitantes dividem o mesmo limite.

### Testes
- **TDD é regra a partir da Fase 04:** regra, AppService, entidade ou endpoint novo/alterado nasce com teste em `tests/` (BE-18); correção de bug começa com um teste que reproduz o bug.
- Um projeto de unitários por serviço (`Climato.{Gateway,Provider}.UnitTests`); pastas espelham `src/`. `tests/Directory.Build.props` traz xUnit v3 e Shouldly; versões no `Directory.Packages.props`.
- Nome `Metodo_Cenario_ResultadoEsperado` em português; corpo Arrange/Act/Assert separado por linha em branco, sem comentários de seção. `TestContext.Current.CancellationToken` como token (também nos `DidNotReceiveWithAnyArgs`, para o analisador xUnit1051).
- Dependências com NSubstitute; sem rede, Docker ou chave real. Métodos privados testados pelo comportamento público (sem `InternalsVisibleTo`). Mapper de teste com `TypeAdapterConfig` própria (`TestMapper`).
- Testes de integração (`WebApplicationFactory` + Testcontainers + WireMock) estão em backlog: [`integracao-plan.md`](../features/04-testes/integracao-plan.md).

### Configuração e segredos
- `appsettings.json` versionado **sem segredos**; valores locais em `appsettings.Development.json` (ignorado) ou `dotnet user-secrets`; em container, variáveis de ambiente (`OpenWeather__ApiKey`).
- Seções tipadas com `IOptions<T>` e `ValidateOnStart()` para chaves obrigatórias.
- Containers: imagens multi-stage (`sdk:10.0` → `aspnet:10.0`, usuário não-root), HTTP na porta 8080, `ASPNETCORE_ENVIRONMENT=Production`; configuração só por variáveis de ambiente (`docker-compose.yml` lê do `.env`, que não é versionado).

### Injeção de dependência por camada
- Cada projeto expõe `DependencyInjection.cs` com uma extensão `Add<Servico><Camada>()` (`AddGatewayApi(configuration)`, `AddGatewayApplication`, `AddGatewayInfrastructure(configuration)`, `AddProviderApi(configuration)`, `AddProviderApplication(configuration)`).
- `Program.cs` só chama essas extensões, configura o pipeline e (no Gateway) aplica migrations conforme `Database:MigrateOnStartup`.
- Options: classe `*Options` com `const string SectionName`, registrada com `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`; mensagens de `[Required]` em português citando a chave.

## Decisões Técnicas

### Gateway como BFF
O frontend só conhece o Gateway. Novos dados para o front (previsão, autocomplete) são expostos pelo Gateway e buscados no Provider, mesmo quando o Gateway só repassa. Ver [`system-design.md`](system-design.md).

### Provider como camada anticorrupção
O formato da OpenWeather/Pexels não vaza para o Gateway: o Provider devolve contratos próprios (`Weather`, previsão, cidades). Trocar o provedor de clima afeta só o Provider. No Gateway, o contrato recebido do Provider é o `WeatherDto` da Application; o Domain do Gateway não conhece o formato do Provider. A previsão é agregada por dia no Provider (data local da cidade, mín/máx, condição do ponto mais próximo de 12:00, maior `pop` × 100, maior vento em km/h); o Gateway e o front não veem os pontos de 3 h.

### Autenticação
JWT HS256 emitido pelo Gateway em `POST /auth/register` e `POST /auth/login` (claims `sub`, `email`, `name`, `jti`; validade `Jwt:ExpirationMinutes`). Sem refresh token ([ADR 0006](decisions/0006-jwt-localstorage-sem-refresh.md)). Senha com `PasswordHasher<User>` (PBKDF2), rehash no login quando o formato envelhece. E-mail normalizado (`User.NormalizeEmail`). Recurso de outro usuário responde 404, nunca 403. Pipeline: `UseSerilogRequestLogging` → `CorrelationIdMiddleware` → `UseExceptionHandler` → `UseStatusCodePages` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization`.

### Histórico e usuário de demonstração
- `GET /weathers/{nome}` só consulta (com `?image=false` o Provider não busca foto). O detalhe do web registra a cidade com `POST /cities` (`{ name }`): o Gateway resolve o nome no Provider (`GetWeatherAsync(nome, includeImage: false)`) e grava o nome e o país devolvidos — texto livre não entra no histórico; cidade inexistente → 404. Idempotente: 201 com a cidade nova, 200 com a existente.
- `Demo:Enabled` liga `POST /auth/demo` (desligado → 404 "Demonstração desativada."). Ninguém se cadastra com `Demo:Email` (409, com a demo ligada ou não). O usuário `Demo:Email` é criado **sob demanda** no primeiro acesso, com senha aleatória descartada e as cidades de `Demo:Cities`; se um visitante excluir a conta, o próximo acesso recria. Os dados da demo são compartilhados entre visitantes.

### Palavra reservada `user`
A tabela se chama `user`, reservada no PostgreSQL: em SQL manual, sempre `"user"`.

### Migrations
EF Core Migrations em `Gateway.Infrastructure/Data/Migrations/`. Mudança de entidade ou configuration → nova migration no mesmo PR. Aplicadas no startup quando `Database:MigrateOnStartup` é `true` (padrão: `true` só em `Development`; o compose liga explicitamente); comandos no [`runbook.md`](../runbook.md#3-banco-de-dados).

## Verificação

Comandos atuais na seção [Verificação do CLAUDE.md](../../CLAUDE.md#verificação).

## Ao concluir

- Padrão novo → esta página + linha nova no [`review-checklist.md`](review-checklist.md).
- Decisão que alguém poderia questionar → ADR.
