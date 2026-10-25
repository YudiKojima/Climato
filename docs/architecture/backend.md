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
| Testes | — | xUnit, NSubstitute, Testcontainers, WireMock.Net (Fase 04) |
| Logs | padrão do ASP.NET | Serilog (Fase 06) |

## Estrutura da Solução

Cada serviço segue camadas no estilo Clean Architecture:

```
Climato.slnx                         ← solução única; Directory.Build.props / Directory.Packages.props / global.json na raiz
src/Gateway/
├── Climato.Gateway.Api/             ← Program.cs, DependencyInjection.cs, Controllers, Filters, Errors, Extensions, OpenApi
├── Climato.Gateway.Application/     ← Contracts, Services, Model (DTOs: Auth, Cities, Users, Weathers), Mappings (Mapster), Validators, Options, Exceptions, DependencyInjection.cs
├── Climato.Gateway.Domain/          ← Entidades e interfaces de repositório por agregado (Cities/, Users/), Exceptions/
└── Climato.Gateway.Infrastructure/  ← Data (ClimatoDbContext, Configurations, Migrations, Repositories), Http (clients, ProviderHealthCheck), Auth (JwtTokenService), DependencyInjection.cs
src/Provider/
├── Climato.Provider.Api/            ← Program.cs, DependencyInjection.cs, Controllers, Errors
├── Climato.Provider.Application/    ← Clientes das APIs externas e cache (Services), Contracts, Model (DTOs da OpenWeather/Pexels), Exceptions, Options, DependencyInjection.cs
└── Climato.Provider.Domain/         ← Weather (contrato devolvido ao Gateway)
```

Testes em `tests/` (Fase 04).

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
- Rotas no plural e minúsculas (`/cities`, `/weathers`, `/auth`).
- `[Authorize]` em todo controller; `[AllowAnonymous]` explícito só em registro, login e health checks. O usuário atual vem das claims, nunca de parâmetro.
- Usuário atual: `User.GetUserId()` (`Api/Extensions/ClaimsPrincipalExtensions`), que lê a claim `sub`.

### DTOs (Application)
- Ficam em `Application/Model/<Agregado>/`; sufixos `Dto` (resposta), `PostDto`/`PutDto` (entrada pelo corpo), `Query` (entrada pela query string, recebida com `[FromQuery]`, ex.: `LocationSearchQuery`). Propriedades de `Query` são anuláveis: com `string` não anulável o `[ApiController]` aplica `[Required]` implícito e responde em inglês antes do validator.
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
- Resiliência com `AddStandardResilienceHandler()` em todo typed client. Timeouts escalonados: a tentativa de quem chama é maior que a soma dos timeouts totais das dependências que o chamado consulta em sequência (Gateway → Provider 25 s/30 s > OpenWeather 15 s + Pexels 5 s + folga do cache; Provider → OpenWeather 5 s/15 s; Provider → Pexels 2 s/5 s). `CircuitBreaker.SamplingDuration` ≥ 2× o timeout por tentativa. O Gateway só re-tenta falha de transporte (`Retry.ShouldHandle` com `HttpRequestException`); 5xx do Provider não é re-tentado.
- Client traduz a resposta: 404 → `null`; outro status de erro, `HttpRequestException`, `ExecutionRejectedException` (Polly) ou `JsonException` → exceção tipada de indisponibilidade (`ExternalServiceUnavailableException` no Provider, `ProviderUnavailableException` no Gateway).
- Provider: toda chamada à OpenWeather passa pelo typed client `IOpenWeatherClient` (`OpenWeatherClient`), que concentra a tradução de erros num `GetAsync<T>` privado; AppServices não recebem `HttpClient` da OpenWeather. No Gateway, o `WeatherProviderClient` faz o mesmo para o Provider.

### Erros
- Respostas de erro em `ProblemDetails` (`AddProblemDetails` + `UseStatusCodePages` + `GlobalExceptionHandler`). `DomainException` → 409 com a mensagem; validação → 400 `ValidationProblemDetails`; resto → 500 "Erro interno." sem detalhes. Erros esperados no controller com `Problem(statusCode, title)` ou `NotFound()`.
- Não usar exceção para fluxo esperado (cidade não encontrada → retorno `null`/resultado, controller devolve 404).
- Dependência fora (API externa, Provider) → exceção tipada de indisponibilidade → **503** no `GlobalExceptionHandler`, log `Warning`. Cidade inexistente continua sendo fluxo esperado (`null` → 404).
- Cliente abortou (`OperationCanceledException` com `RequestAborted` cancelado) → 499 sem corpo, log `Information`.
- Violação de constraint que reflete regra (e-mail único) → o repositório traduz para `DomainException` (409); FK de usuário inexistente → `UserNotFoundException` → 401.
- 400 automático do `[ApiController]` usa o mesmo `ValidationProblemDetails` do `ValidationFilter` (`InvalidModelStateResponseFactory`, título "Dados inválidos.").
- Falha opcional (imagem do Pexels, cache) não derruba a resposta: log `Warning` e segue.

### Cache (Provider)
- `IDistributedCache` atrás de `IProviderCache` (`Services/ProviderCache`), genérico: `GetAsync<T>(prefixo, texto)` / `SetAsync<T>(prefixo, texto, valor, duração)`. Redis se `ConnectionStrings:Redis` estiver preenchida; senão `AddDistributedMemoryCache`.
- Chave `{prefixo}:{texto-normalizado}` (trim, espaços colapsados, minúsculas, sem acentos); valor em JSON; TTL absoluto por uso: `clima` 10 min, `previsao` 30 min, `locais` 24 h (lista vazia também é cacheada).
- Só resposta de sucesso entra no cache (404 e falhas não). Erro do cache → `Warning` e segue sem cache.

### Health checks
- `/health/live`: sem checks (processo vivo). `/health/ready`: checks com a tag `ready`.
- Gateway: Postgres (`AddDbContextCheck`) e Provider (`ProviderHealthCheck` → `GET health/ready`, client nomeado sem resiliência, 5 s). Provider: Redis, só se configurado, com `failureStatus: HealthStatus.Degraded` (200; Redis fora não derruba o `ready` do Gateway).
- APIs externas (OpenWeather, Pexels) **não** entram: gastariam cota.
- No Gateway, `MapHealthChecks(...).AllowAnonymous()`.

### Configuração e segredos
- `appsettings.json` versionado **sem segredos**; valores locais em `appsettings.Development.json` (ignorado) ou `dotnet user-secrets`; em container, variáveis de ambiente (`OpenWeather__ApiKey`).
- Seções tipadas com `IOptions<T>` e `ValidateOnStart()` para chaves obrigatórias.

### Injeção de dependência por camada
- Cada projeto expõe `DependencyInjection.cs` com uma extensão `Add<Servico><Camada>()` (`AddGatewayApi(configuration)`, `AddGatewayApplication`, `AddGatewayInfrastructure(configuration)`, `AddProviderApi(configuration)`, `AddProviderApplication(configuration)`).
- `Program.cs` só chama essas extensões, configura o pipeline e (no Gateway) aplica migrations fora de `Production`.
- Options: classe `*Options` com `const string SectionName`, registrada com `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`; mensagens de `[Required]` em português citando a chave.

## Decisões Técnicas

### Gateway como BFF
O frontend só conhece o Gateway. Novos dados para o front (previsão, autocomplete) são expostos pelo Gateway e buscados no Provider, mesmo quando o Gateway só repassa. Ver [`system-design.md`](system-design.md).

### Provider como camada anticorrupção
O formato da OpenWeather/Pexels não vaza para o Gateway: o Provider devolve contratos próprios (`Weather`, previsão, cidades). Trocar o provedor de clima afeta só o Provider. No Gateway, o contrato recebido do Provider é o `WeatherDto` da Application; o Domain do Gateway não conhece o formato do Provider.

### Autenticação
JWT HS256 emitido pelo Gateway em `POST /auth/register` e `POST /auth/login` (claims `sub`, `email`, `name`, `jti`; validade `Jwt:ExpirationMinutes`). Sem refresh token ([ADR 0006](decisions/0006-jwt-localstorage-sem-refresh.md)). Senha com `PasswordHasher<User>` (PBKDF2), rehash no login quando o formato envelhece. E-mail normalizado (`User.NormalizeEmail`). Recurso de outro usuário responde 404, nunca 403. Pipeline: `UseExceptionHandler` → `UseStatusCodePages` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization`.

### Palavra reservada `user`
A tabela se chama `user`, reservada no PostgreSQL: em SQL manual, sempre `"user"`.

### Migrations
EF Core Migrations em `Gateway.Infrastructure/Data/Migrations/`. Mudança de entidade ou configuration → nova migration no mesmo PR. Aplicadas automaticamente no startup fora de `Production`; comandos no [`runbook.md`](../runbook.md#3-banco-de-dados).

## Verificação

Comandos atuais na seção [Verificação do CLAUDE.md](../../CLAUDE.md#verificação).

## Ao concluir

- Padrão novo → esta página + linha nova no [`review-checklist.md`](review-checklist.md).
- Decisão que alguém poderia questionar → ADR.
