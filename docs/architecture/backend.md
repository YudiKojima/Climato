# Backend — Arquitetura e Padrões

Vale para `WeatherGatewayApp/` e `WeatherProviderApp/`. Seções marcadas **(a partir da Fase NN)** descrevem padrões já decididos que ainda não estão no código; ao implementá-los, remover a marcação.

## Stack

| | Atual | Alvo |
|---|---|---|
| Runtime | .NET 8 | .NET 10 (Fase 01, [ADR 0004](decisions/0004-dotnet-10.md)) |
| API | ASP.NET Core com controllers | idem |
| Persistência (Gateway) | EF Core 9 + Npgsql, `new ConnectionContext()` | EF Core + Npgsql via `AddDbContext`, Migrations (Fase 01) |
| Mapeamento | AutoMapper | idem (só no Gateway) |
| JSON externo | Newtonsoft + `JsonDictionary` | `System.Text.Json` + DTOs tipados (Fase 03) |
| Validação | — | FluentValidation (Fase 02) |
| Auth | — | JWT Bearer (Fase 02) |
| Resiliência / cache | — | `Microsoft.Extensions.Http.Resilience` / `IDistributedCache` + Redis (Fase 03) |
| Testes | — | xUnit, NSubstitute, Testcontainers, WireMock.Net (Fase 04) |
| Logs | padrão do ASP.NET | Serilog (Fase 06) |

## Estrutura da Solução

Cada serviço segue camadas no estilo Clean Architecture:

```
WeatherGatewayApp/
├── WeatherGatewayApp/                 ← API: Program.cs, Controllers
├── WeatherGatewayApp.Application/     ← Contracts (I*AppService), Services, Model (DTOs), Core (mapeamentos, extensões)
├── WeatherGatewayApp.Domain/          ← Entidades e interfaces de repositório, por agregado (Cities/, Users/, Weathers/)
└── WeatherGatewayApp.Infrastructure/  ← ConnectionContext (EF), Data/Repositories
WeatherProviderApp/
├── WeatherProviderApp/                ← API
├── WeatherProviderApp.Application/    ← Clientes das APIs externas (Services)
└── WeatherProviderApp.Domain/         ← Weather (contrato devolvido ao Gateway)
```

**Alvo (Fase 01):** solução única `Climato.sln` na raiz com pastas de solução `Gateway/` e `Provider/`; testes em `tests/` (Fase 04).

### Dependências entre camadas

```
API ──▶ Application ──▶ Domain
 └────▶ Infrastructure ──▶ Domain
```

- **Domain** não referencia nada do projeto nem pacotes de infraestrutura.
- **Application** não referencia Infrastructure; usa as interfaces do Domain.
- **API** só referencia Infrastructure para registrar o DI.
- Pacotes ficam no projeto que os usa (EF Core/Npgsql só na Infrastructure).

## Convenções de Código

### Geral
- `Nullable` habilitado; nenhum aviso de nulabilidade novo.
- Nomes de tipos e membros em inglês (padrão .NET); **mensagens ao usuário, exceções e docs em português**.
- Namespace igual ao caminho da pasta, no formato file-scoped (`namespace X;`) em arquivos novos ou tocados.
- Campos privados `camelCase` sem `_`, como no código existente.
- Chaves em todo `if`/`else`/`foreach`.
- Async de ponta a ponta: métodos retornam `Task`/`Task<T>`, nunca `async void`; sem `.Result`/`.Wait()`. `CancellationToken` propagado do controller ao repositório e ao `HttpClient` (a partir da Fase 01).

### Controllers
- Finos: validam a entrada (via FluentValidation, Fase 02), chamam um AppService e traduzem o resultado em status HTTP.
- `ActionResult<T>` com o DTO de resposta; XML doc (`/// <summary>`) em português em cada action — aparece no Swagger.
- Rotas no plural e minúsculas (`/cities`, `/weathers`, `/auth`).
- **(a partir da Fase 02)** `[Authorize]` em todo controller; `[AllowAnonymous]` explícito só em registro, login e health checks. O usuário atual vem das claims, nunca de parâmetro.

### DTOs (Application)
- Ficam em `Application/Model/<Agregado>/`; sufixos `Dto` (resposta), `PostDto`/`PutDto` (entrada).
- DTO de resposta **nunca** expõe senha, hash ou dado interno.
- Entidades do Domain nunca saem pelo controller.

### Entidades (Domain)
- Construtor com os campos obrigatórios gera `Id` e `CreatedAt`; construtor vazio só para o EF.
- Regra que muda estado da entidade é método dela (ex.: `city.ToggleFavorite()`), não lógica solta no AppService.

### Repositórios (Infrastructure)
- Interface no Domain, implementação na Infrastructure.
- Recebem o `ConnectionContext` pelo construtor (**a partir da Fase 01**; hoje usam `new` — A10).
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

## Decisões Técnicas

### Gateway como BFF
O frontend só conhece o Gateway. Novos dados para o front (previsão, autocomplete) são expostos pelo Gateway e buscados no Provider, mesmo quando o Gateway só repassa. Ver [`system-design.md`](system-design.md).

### Provider como camada anticorrupção
O formato da OpenWeather/Pexels não vaza para o Gateway: o Provider devolve contratos próprios (`Weather`, previsão, cidades). Trocar o provedor de clima afeta só o Provider.

### Palavra reservada `user`
A tabela se chama `user`, reservada no PostgreSQL: em SQL manual, sempre `"user"`.

### Migrations **(a partir da Fase 01)**
EF Core Migrations no projeto Infrastructure do Gateway. Mudança de entidade ou mapping → nova migration no mesmo PR. O script `Scripts/170520251436.sql` é aposentado.

## Verificação

Comandos atuais na seção [Verificação do CLAUDE.md](../../CLAUDE.md#verificação).

## Ao concluir

- Padrão novo → esta página + linha nova no [`review-checklist.md`](review-checklist.md).
- Decisão que alguém poderia questionar → ADR.
