# Design — Fase 01: Higiene e fundação

**Data:** 2026-10-16
**Fase:** 01
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#01--higiene-e-fundação) · [auditoria](../00-roteiro-portfolio/auditoria-inicial.md)
**Branch:** `dev/higiene-fundacao`

---

## Objetivo

Deixar o projeto **compilável e executável a partir de um clone**, com estrutura, configuração e camadas corretas, sem mudar o comportamento visível do app. É a base sobre a qual as fases 02–08 constroem.

**Auditoria resolvida:** A5, A8, A9, A10, A11, A12, A13, A20, A21, A22, A23 (parte de solução/config; Docker é da Fase 05), A27 (só a URL da API), **A28** (novo, ver abaixo).

**Fora do escopo** (mesmo se tocado pela reorganização):
- Senha em texto puro, senha no `UserDto`, autenticação, CORS (A1–A4, A6, A7) → Fase 02
- `JsonDictionary`/Newtonsoft, DTOs tipados, mapeamento de erros, unidades e fuso (A14–A17, A19) → Fase 03
- Visual do front → Fase 07

## Decisões

| Tema | Decisão |
|---|---|
| Estrutura | Reorganizar em `src/Gateway` e `src/Provider`, projetos e namespaces `Climato.*` |
| Solução | `Climato.slnx` na raiz + `Directory.Build.props`, `Directory.Packages.props` (Central Package Management) e `global.json` |
| Runtime | `net10.0` ([ADR 0004](../../architecture/decisions/0004-dotnet-10.md)); EF Core 10 + Npgsql 10 |
| Documentação da API | OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) + **Scalar**, só em `Development` |
| Migrations | EF Core Migrations; aplicadas **automaticamente no startup fora de `Production`**; `dotnet ef database update` disponível |
| Banco local existente | Recriado do zero (sem script de conversão) |
| Mapeamento | **Mapster** no lugar do AutoMapper ([ADR 0005](../../architecture/decisions/0005-mapster.md)), modo runtime |
| Segredos locais | `dotnet user-secrets`; `appsettings.json` versionado sem segredos |
| Execução | Kestrel via `dotnet run`, sem IIS Express. Gateway `https://localhost:7001`, Provider `https://localhost:7002` |
| PR | Um PR, commits em ordem lógica (ver [Sequência de commits](#sequência-de-commits)) |

## Achado novo

| Código | Sev. | Achado | Fase |
|---|---|---|---|
| A28 | 🟡 | Camadas invertidas: `Gateway.Application` referencia `Gateway.Infrastructure`; EF Core/Npgsql/AutoMapper no Domain dos dois serviços; o cliente HTTP do Provider está na Application | 01 |

Registrar na [auditoria](../00-roteiro-portfolio/auditoria-inicial.md) na tabela "Código e estrutura".

---

## 1. Estrutura e build

```
Climato.slnx
Directory.Build.props        ← TargetFramework net10.0, Nullable, ImplicitUsings, LangVersion latest
Directory.Packages.props     ← ManagePackageVersionsCentrally + versões
global.json                  ← sdk 10.0.x, rollForward latestFeature
src/
├── Gateway/
│   ├── Climato.Gateway.Api/              (era WeatherGatewayApp/WeatherGatewayApp)
│   ├── Climato.Gateway.Application/
│   ├── Climato.Gateway.Domain/
│   └── Climato.Gateway.Infrastructure/
└── Provider/
    ├── Climato.Provider.Api/             (era WeatherProviderApp/WeatherProviderApp)
    ├── Climato.Provider.Application/
    └── Climato.Provider.Domain/
web/
```

- Pastas de solução no `.slnx`: `Gateway`, `Provider`, `Solution Items` (props, `global.json`, `.gitignore`).
- **Removidos:** `WeatherProviderApp.Infrastructure` (A12), `WeatherGatewayApp/Scripts/` (substituído pela migration), `*.csproj.user`, `.editorconfig` duplicados (um único `.editorconfig` na raiz, a partir do existente).
- `.csproj` só com `PackageReference` sem versão e `ProjectReference`.
- Namespaces file-scoped `Climato.Gateway.*` / `Climato.Provider.*` (A13) — na reorganização, apenas troca de nome; o resto do conteúdo não muda no commit 1.

### Dependências corrigidas (A28)

| Projeto | Referencia | Pacotes |
|---|---|---|
| `Gateway.Domain` | — | — |
| `Gateway.Application` | Domain | Mapster, Mapster.DependencyInjection, `Microsoft.Extensions.Options` (abstrações) |
| `Gateway.Infrastructure` | Domain, Application | EF Core, Npgsql.EntityFrameworkCore.PostgreSQL, EF Core Design (migrations), `Microsoft.Extensions.Http` |
| `Gateway.Api` | Application, Infrastructure | Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore |
| `Provider.Domain` | — | — |
| `Provider.Application` | Domain | Newtonsoft.Json (até a Fase 03), `Microsoft.Extensions.Http`, `Microsoft.Extensions.Options` |
| `Provider.Api` | Application | Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore |

O Provider continua sem Infrastructure: os clientes das APIs externas ficam na Application até a Fase 03 decidir o contrário.

### `.gitignore` (A23)

Reescrito em seções (.NET, Node/Vite, IDEs, SO, Claude Code). **Passa a versionar:** `*.slnx`, `appsettings.json`, `Properties/launchSettings.json`, `Dockerfile`, `.dockerignore`. **Continua ignorando:** `bin/`, `obj/`, `appsettings.Development.json`, `appsettings.Local.json`, `.env`, `.env.local`, `node_modules/`, `dist/`.

### `launchSettings.json`

Um perfil `https` por API, `launchUrl: "scalar"`, `ASPNETCORE_ENVIRONMENT=Development`:

| API | URLs |
|---|---|
| Gateway | `https://localhost:7001;http://localhost:5001` |
| Provider | `https://localhost:7002;http://localhost:5002` |

Remove o perfil com IP de rede local e o IIS Express.

## 2. Configuração, DI e dados

### Options

| Classe | Seção | Serviço | Valor versionado (`appsettings.json`) | Segredo (user-secrets) | Validação |
|---|---|---|---|---|---|
| — | `ConnectionStrings:Climato` | Gateway | — | `Host=localhost;Port=5432;Database=climato;Username=postgres;Password=...` | `GetConnectionString` não vazio, senão falha no startup |
| `ProviderOptions` | `Provider` | Gateway | `BaseUrl: https://localhost:7002` | — | `[Required]`, `[Url]` |
| `CorsOptions` | `Cors` | Gateway | `AllowedOrigins: ["http://localhost:5173"]` | — | — (aplicado na Fase 02) |
| `OpenWeatherOptions` | `OpenWeather` | Provider | `BaseUrl: https://api.openweathermap.org/` | `ApiKey` | `[Required]` em ambos |
| `PexelsOptions` | `Pexels` | Provider | `BaseUrl: https://api.pexels.com/` | `ApiKey` | `[Required]` em ambos |

Registro: `AddOptions<T>().BindConfiguration(T.SectionName).ValidateDataAnnotations().ValidateOnStart()`. Classes `*Options` ficam na Application do serviço (são contratos de configuração consumidos por ela).

O nome do banco muda de `weather-app` para `climato` (banco é recriado de qualquer forma).

### DI por camada

```csharp
// Gateway.Api/Program.cs
builder.Services
    .AddGatewayApplication()
    .AddGatewayInfrastructure(builder.Configuration);

// Provider.Api/Program.cs
builder.Services.AddProviderApplication();
```

Cada projeto tem `DependencyInjection.cs` com sua extensão. `Program.cs` fica com: controllers, OpenAPI/Scalar, extensões de camada, pipeline e migration em dev.

### Dados (Gateway)

- `ConnectionContext` → **`ClimatoDbContext`**, construtor com `DbContextOptions<ClimatoDbContext>`, registrado com `AddDbContext` + `UseNpgsql(connectionString)` (A5, A10).
- Mapeamento em `Infrastructure/Data/Configurations/{City,User}Configuration.cs` (`IEntityTypeConfiguration<T>`), aplicados com `ApplyConfigurationsFromAssembly`. Atributos `[Table]`/`[Key]` saem das entidades.
- Nomes físicos **iguais aos atuais**: tabelas `city`, `user`; colunas `id`, `userid`, `name`, `country`, `isfavorite`, `createdat`, `email`, `password`.
- Migration **`Inicial`** em `Infrastructure/Data/Migrations/` (A9):
  - `user` criada antes de `city`; FK `city.userid → user.id` `ON DELETE CASCADE`
  - `createdat` `timestamp with time zone`, default `now()`
  - índice único em `user.email`; índice em `city.userid`
- Repositórios recebem `ClimatoDbContext` pelo construtor, registrados como `Scoped`.
- Startup (`Program.cs`): fora de `Production`, `await db.Database.MigrateAsync()` num escopo criado logo após o `Build()`.

### HTTP Gateway → Provider (A11, A20, A28)

- Interface `IWeatherProviderClient` em `Gateway.Application/Contracts/`; implementação `WeatherProviderClient` em `Gateway.Infrastructure/Http/` (move a chamada HTTP que hoje está no `WeatherAppService`).
- `services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>((sp, c) => c.BaseAddress = new Uri(options.BaseUrl))` — um registro só.
- O `WeatherAppService` passa a depender de `IWeatherProviderClient` e `ICityRepository`. O parsing com `JsonDictionary` vai junto para o client **sem mudanças** (Fase 03).
- Provider: `AddHttpClient<IWeatherAppService, WeatherAppService>` e `AddHttpClient<ICityImageAppService, CityImageAppService>` com `BaseAddress` dos Options; remove `AddTransient` duplicados. Chaves lidas de `IOptions<T>`, não de `IConfiguration`. Query com `Uri.EscapeDataString(cityName)`.

### `CancellationToken`

Propagado de toda action → AppService → repositório (`ToListAsync(ct)`, `SaveChangesAsync(ct)`) / client HTTP (`GetAsync(url, ct)`). Nas interfaces (AppServices, repositórios, clients), o parâmetro `CancellationToken cancellationToken` é **obrigatório, sem valor default**, para que nenhum chamador esqueça de repassá-lo.

### OpenAPI

`builder.Services.AddOpenApi()`; em `Development`: `app.MapOpenApi()` + `app.MapScalarApiReference()`. `GenerateDocumentationFile` ligado nos projetos Api (com `NoWarn` 1591) para os `/// <summary>` aparecerem.

## 3. Correções, mapeamento e frontend

### Bugs

| Código | Correção |
|---|---|
| A8 | `ICityAppService.DeleteAsync` e `IUserAppService.DeleteAsync` → `Task<bool>`; controllers `await` e respondem `204 NoContent` / `404 NotFound`. Nenhum `async void` restante |
| A20/A21 | Ver seções 1 e 2 |

Comportamento observável preservado, exceto: `DELETE` passa de `200` para `204`/`404`.

### Mapster (ADR 0005)

- `Gateway.Application/Mappings/CityMappings.cs` e `UserMappings.cs` implementando `IRegister`.
- `AddGatewayApplication()`: `TypeAdapterConfig.GlobalSettings.Scan(assembly)`, `services.AddSingleton(config)`, `services.AddScoped<IMapper, ServiceMapper>()`.
- AppServices trocam `AutoMapper.IMapper` por `MapsterMapper.IMapper` — chamadas `mapper.Map<T>(obj)` iguais.
- Mapeamento `User → UserPostDto` (sem uso) é removido.
- `UserDto` **mantém** `Password` nesta fase (A3 é da Fase 02); a Fase 02 remove.

### Frontend (A27 parcial)

- `web/src/services/api.js`: `const API = import.meta.env.VITE_API_URL;`
- `web/.env.example` versionado: `VITE_API_URL=https://localhost:7001`. `web/.env` local ignorado.
- Nenhuma outra mudança no front.

## 4. Documentação

| Arquivo | Mudança |
|---|---|
| `docs/runbook.md` | Reescrito: pré-requisitos (.NET 10, Node, Postgres), `dotnet user-secrets set ...` por serviço, `dotnet run --project`, `web/.env`, portas 7001/7002/5173, recriar banco |
| `CLAUDE.md` | Seção Verificação: `dotnet build Climato.slnx`; caminhos `src/Gateway`, `src/Provider` nas tabelas |
| `llms.txt` | Caminhos novos |
| `docs/architecture/backend.md` | Estrutura `src/`, tabela de dependências, Options, DI por camada, Mapster, migrations; remover marcações "(a partir da Fase 01)"; atualizar a tabela Stack |
| `docs/architecture/system-design.md` | Diagrama atual com a URL configurável |
| `docs/architecture/review-checklist.md` | Caminhos novos nas regras; **BE-19** 🟡 "Mapeamento fora de `Mappings/`" (`Adapt`/config Mapster fora de `Application/Mappings/`); **BE-20** 🔴 "Options sem validação" (seção obrigatória sem `ValidateOnStart`) |
| `.claude/skills/revisar-padrao/SKILL.md` | Caminhos `src/**` |
| `docs/architecture/decisions/0005-mapster.md` | Novo ADR |
| `docs/features/00-roteiro-portfolio/auditoria-inicial.md` | A28 adicionado; ✅ nos códigos resolvidos |
| `docs/project-overview.md` | Stack, estrutura, Fase 01 `✅ Concluída` |

## Sequência de commits

Cada commit compila (`dotnet build Climato.slnx`).

1. **Reorganiza projetos em `src/` com namespaces `Climato.*`** — `git mv`, renomear `.csproj`, `Climato.slnx`, namespaces/usings. Sem outras mudanças.
2. **Atualiza para .NET 10 com gerenciamento central de pacotes** — `Directory.*.props`, `global.json`, `.gitignore`, `.editorconfig`, `launchSettings.json`, pacotes; Swashbuckle → OpenAPI + Scalar; AutoMapper → Mapster; remove Infrastructure órfão e `*.csproj.user`.
3. **Corrige camadas, configuração e acesso a dados** — A28, Options, DI por camada, `ClimatoDbContext`, configurations, migration `Inicial`, typed clients, `CancellationToken`, remove `Scripts/`.
4. **Corrige exclusões assíncronas e URL da API no front** — A8, `VITE_API_URL`.
5. **Atualiza documentação da Fase 01** — tabela acima.

## Verificação

1. `dotnet build Climato.slnx` sem erros; `npm --prefix web run lint`; `npm --prefix web run build`.
2. **Clone limpo:** `git clone` da branch numa pasta temporária → `dotnet build Climato.slnx` passa (prova do A23).
3. **Startup sem segredo:** Provider sem `OpenWeather:ApiKey` falha ao subir com mensagem citando a chave.
4. **Roteiro manual** com banco `climato` inexistente:
   - `dotnet run` do Gateway cria o banco e as tabelas (conferir FK e índices no `psql \d city`)
   - Provider + `npm --prefix web run dev`
   - cadastro → login → buscar "Cuiabá" (clima + imagem) → cidade no histórico → favoritar → desfavoritar → excluir (DevTools: `204`) → excluir id inexistente via Scalar (`404`)
   - Scalar abre em `https://localhost:7001/scalar` e `https://localhost:7002/scalar` com os summaries em português

## Riscos

| Risco | Mitigação |
|---|---|
| `git mv` + edição no mesmo commit confunde o rastreio de renomeação | Commit 1 só move/renomeia; conteúdo muda a partir do commit 2 |
| Quebras de API ao pular de EF Core 9 → 10 / Npgsql 10 | Modelo simples (2 entidades); build + roteiro manual cobrem |
| Scalar/OpenAPI sem os XML docs | `GenerateDocumentationFile` nos projetos Api; verificação 4 confere |
| Alterações locais não commitadas em `ConnectionContext.cs` e no script SQL | Descartar antes do commit 1 (arquivos são substituídos nesta fase) — confirmar com o usuário |
