# Backend — Arquitetura e Padrões

Vale para `src/Gateway/` e `src/Provider/`. Seções marcadas **(a partir da Fase NN)** descrevem padrões já decididos que ainda não estão no código; ao implementá-los, remover a marcação.

## Stack

| | Atual | Alvo |
|---|---|---|
| Runtime | .NET 10 ([ADR 0004](decisions/0004-dotnet-10.md)) | — |
| API | ASP.NET Core com controllers; OpenAPI nativo + Scalar | — |
| Persistência (Gateway) | EF Core 10 + Npgsql via `AddDbContext`, Migrations | — |
| Mapeamento | Mapster, só no Gateway ([ADR 0005](decisions/0005-mapster.md)) | — |
| JSON externo | Newtonsoft + `JsonDictionary` | `System.Text.Json` + DTOs tipados (Fase 03) |
| Validação | — | FluentValidation (Fase 02) |
| Auth | — | JWT Bearer (Fase 02) |
| Resiliência / cache | — | `Microsoft.Extensions.Http.Resilience` / `IDistributedCache` + Redis (Fase 03) |
| Testes | — | xUnit, NSubstitute, Testcontainers, WireMock.Net (Fase 04) |
| Logs | padrão do ASP.NET | Serilog (Fase 06) |

## Estrutura da Solução

Cada serviço segue camadas no estilo Clean Architecture:

```
Climato.slnx                         ← solução única; Directory.Build.props / Directory.Packages.props / global.json na raiz
src/Gateway/
├── Climato.Gateway.Api/             ← Program.cs, Controllers
├── Climato.Gateway.Application/     ← Contracts, Services, Model (DTOs: Cities, Users, Weathers), Mappings (Mapster), Options, DependencyInjection.cs
├── Climato.Gateway.Domain/          ← Entidades e interfaces de repositório por agregado (Cities/, Users/)
└── Climato.Gateway.Infrastructure/  ← Data (ClimatoDbContext, Configurations, Migrations, Repositories), Http (clients), DependencyInjection.cs
src/Provider/
├── Climato.Provider.Api/
├── Climato.Provider.Application/    ← Clientes das APIs externas (Services), Options, DependencyInjection.cs, Core/Collections (JsonDictionary, até a Fase 03)
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
- Finos: validam a entrada (via FluentValidation, Fase 02), chamam um AppService e traduzem o resultado em status HTTP.
- `ActionResult<T>` com o DTO de resposta; XML doc (`/// <summary>`) em português em cada action — aparece na documentação OpenAPI/Scalar.
- Rotas no plural e minúsculas (`/cities`, `/weathers`, `/auth`).
- **(a partir da Fase 02)** `[Authorize]` em todo controller; `[AllowAnonymous]` explícito só em registro, login e health checks. O usuário atual vem das claims, nunca de parâmetro.

### DTOs (Application)
- Ficam em `Application/Model/<Agregado>/`; sufixos `Dto` (resposta), `PostDto`/`PutDto` (entrada).
- DTO de resposta **nunca** expõe senha, hash ou dado interno (exceção conhecida: `UserDto.Password`, removido na Fase 02 — A3).
- Entidades do Domain nunca saem pelo controller.

### Mapeamento (Mapster)
- Configuração em `Application/Mappings/` com uma classe `IRegister` por agregado (`CityMappings`, `UserMappings`).
- AppServices recebem `MapsterMapper.IMapper` e chamam `mapper.Map<TDestino>(origem)`. Nada de `.Adapt()` ou `TypeAdapterConfig` fora de `Mappings/` e do `DependencyInjection.cs`.
- Decisão: [ADR 0005](decisions/0005-mapster.md).

### Entidades (Domain)
- Construtor com os campos obrigatórios gera `Id` e `CreatedAt`; construtor vazio só para o EF — `User` ainda é montado campo a campo pelo `UserAppService` (ajuste na Fase 02).
- Regra que muda estado da entidade é método dela (ex.: `city.ToggleFavorite()`), não lógica solta no AppService.

### Repositórios (Infrastructure)
- Interface no Domain, implementação na Infrastructure.
- Recebem o `ClimatoDbContext` pelo construtor e são registrados como `Scoped`. Todo método recebe `CancellationToken`.
- Consultas filtradas por usuário recebem `userId` explícito; nunca retornar dados de outro usuário.

### Comunicação HTTP
- **Typed HttpClient** registrado com `AddHttpClient<IContrato, Implementacao>()`, sem `AddTransient` duplicado (A20).
- `BaseAddress` e chaves vêm de configuração (`Options`), nunca literais no código.
- Parâmetros de query sempre com `Uri.EscapeDataString`.
- **(a partir da Fase 03)** DTOs tipados por API externa (`OpenWeatherCurrentResponse` etc.) desserializados com `ReadFromJsonAsync`; resiliência com `AddStandardResilienceHandler()`.

### Erros
- **(a partir da Fase 02)** Respostas de erro em `ProblemDetails` (`AddProblemDetails` + handler global de exceções). Exceções de domínio mapeadas para 400/404/409; o resto vira 500 sem detalhes internos.
- Não usar exceção para fluxo esperado (cidade não encontrada → retorno `null`/resultado, controller devolve 404).

### Configuração e segredos
- `appsettings.json` versionado **sem segredos**; valores locais em `appsettings.Development.json` (ignorado) ou `dotnet user-secrets`; em container, variáveis de ambiente (`OpenWeather__ApiKey`).
- Seções tipadas com `IOptions<T>` e `ValidateOnStart()` para chaves obrigatórias.

### Injeção de dependência por camada
- Cada projeto expõe `DependencyInjection.cs` com uma extensão `Add<Servico><Camada>()` (`AddGatewayApplication`, `AddGatewayInfrastructure(configuration)`, `AddProviderApplication`).
- `Program.cs` só chama essas extensões, configura o pipeline e (no Gateway) aplica migrations fora de `Production`.
- Options: classe `*Options` com `const string SectionName`, registrada com `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`; mensagens de `[Required]` em português citando a chave.

## Decisões Técnicas

### Gateway como BFF
O frontend só conhece o Gateway. Novos dados para o front (previsão, autocomplete) são expostos pelo Gateway e buscados no Provider, mesmo quando o Gateway só repassa. Ver [`system-design.md`](system-design.md).

### Provider como camada anticorrupção
O formato da OpenWeather/Pexels não vaza para o Gateway: o Provider devolve contratos próprios (`Weather`, previsão, cidades). Trocar o provedor de clima afeta só o Provider. No Gateway, o contrato recebido do Provider é o `WeatherDto` da Application; o Domain do Gateway não conhece o formato do Provider.

### Palavra reservada `user`
A tabela se chama `user`, reservada no PostgreSQL: em SQL manual, sempre `"user"`.

### Migrations
EF Core Migrations em `Gateway.Infrastructure/Data/Migrations/`. Mudança de entidade ou configuration → nova migration no mesmo PR. Aplicadas automaticamente no startup fora de `Production`; comandos no [`runbook.md`](../runbook.md#3-banco-de-dados).

## Verificação

Comandos atuais na seção [Verificação do CLAUDE.md](../../CLAUDE.md#verificação).

## Ao concluir

- Padrão novo → esta página + linha nova no [`review-checklist.md`](review-checklist.md).
- Decisão que alguém poderia questionar → ADR.
