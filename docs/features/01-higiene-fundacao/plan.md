# Fase 01 — Higiene e fundação: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** deixar o Climato compilável e executável a partir de um clone, com estrutura `src/`, .NET 10, configuração tipada, DbContext via DI, EF Migrations e camadas corretas, conforme o [design aprovado](design.md).

**Arquitetura:** a reorganização (`git mv` + troca de nome) vai num commit isolado. Depois vêm tooling e build (.NET 10, CPM, OpenAPI/Scalar), a correção de camadas e dados do Gateway, Options e typed clients do Provider, as exclusões assíncronas mais a URL do front e, por fim, a documentação. Comportamento visível preservado, exceto `DELETE` → `204`/`404`.

**Stack:** .NET SDK 10.0.301 · EF Core 10.0.13 · Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 · Microsoft.AspNetCore.OpenApi 10.0.13 · Scalar.AspNetCore 2.18.0 · Mapster 10.0.13 · React 19 + Vite 6.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) e [`frontend.md`](../../architecture/frontend.md) antes de editar. O design vale mais que este plano em caso de conflito.
- **Não há projeto de testes** (Fase 04). Cada tarefa termina com `dotnet build Climato.slnx` sem erros (a partir da Tarefa 1); verificação funcional na Tarefa 7.
- **Commits:** a sequência de commits foi aprovada no design — é a autorização explícita para commitar ao fim de cada tarefa (exceção à regra "nunca commitar durante a fase" do `CLAUDE.md`). Antes de cada commit, rodar `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/higiene-fundacao` (já criada a partir do `main`).
- **Nunca** colocar valores de segredo em arquivo versionado nem exibi-los no terminal/chat.
- Fora do escopo: A1–A4, A6, A7 (Fase 02); A14–A17, A19 (Fase 03). `UserDto.Password`, `JsonDictionary` e CORS `AllowAll` **permanecem**.
- Código C#: chaves em todo `if`/`else`/`foreach`; campos privados `camelCase` sem `_`; namespace file-scoped; mensagens em português.

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| Mapster no commit 2 | Mapster no commit 3 (Tarefa 3) | Fica junto da DI por camada, que é onde ele é registrado; AutoMapper 14 compila em `net10.0` até lá |
| Commit 3 único | Tarefas 3 (Gateway) e 4 (Provider), um commit cada | Diffs menores; total de 6 commits |
| `CorsOptions` + seção `Cors` | Não criados | Nada os consome antes da Fase 02 (YAGNI); a Fase 02 cria |
| EF Core Design na Infrastructure | No projeto Api (`PrivateAssets=all`) | `dotnet ef` exige o pacote Design no *startup project* |
| `Microsoft.Extensions.Options` na Gateway.Application | Não referenciado | `ProviderOptions` só usa `System.ComponentModel.DataAnnotations` (BCL); quem registra é a Infrastructure |

## Mapa de arquivos

| Arquivo (caminho final) | Ação | Tarefa |
|---|---|---|
| `src/Gateway/**`, `src/Provider/**` | Mover (`git mv`) + renomear | 1 |
| `Climato.slnx` | Criar | 1 |
| `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.config/dotnet-tools.json` | Criar | 2 |
| `.gitignore` | Reescrever | 2 |
| `src/*/*/*.csproj` | Reescrever | 2, 3 |
| `src/*/Climato.*.Api/Properties/launchSettings.json`, `appsettings.json` | Reescrever (passam a ser versionados) | 2 |
| `src/*/Climato.*.Api/Program.cs` | Modificar / reescrever | 2, 3, 4 |
| `WeatherProviderApp/` (Infrastructure órfão, `.editorconfig`, `*.csproj.user`), `WeatherGatewayApp/.editorconfig` | Remover | 2 |
| `src/Gateway/Climato.Gateway.Domain/{Cities,Users}/*.cs` | Reescrever | 3 |
| `src/Gateway/Climato.Gateway.Application/**` | Reescrever / criar / remover | 3, 5 |
| `src/Gateway/Climato.Gateway.Infrastructure/**` | Reescrever / criar | 3 |
| `src/Gateway/Climato.Gateway.Api/Controllers/*.cs` | Reescrever | 3, 5 |
| `WeatherGatewayApp/Scripts/` | Remover | 3 |
| `src/Provider/Climato.Provider.Application/**` | Reescrever / criar | 4 |
| `src/Provider/Climato.Provider.Api/Controllers/WeatherController.cs` | Reescrever | 4 |
| `web/src/services/api.js`, `web/.env.example` | Modificar / criar | 5 |
| `docs/**`, `CLAUDE.md`, `llms.txt`, `.claude/skills/revisar-padrao/SKILL.md` | Modificar / criar | 6 |

---

### Tarefa 1: Reorganizar projetos em `src/` com namespaces `Climato.*` (A13)

Só movimentação e troca de nomes — nenhuma outra mudança de conteúdo (preserva a detecção de renomeação do git).

**Arquivos:** todos os projetos; criar `Climato.slnx`.

**Interfaces:**
- Produz: projetos `Climato.Gateway.{Api,Application,Domain,Infrastructure}` e `Climato.Provider.{Api,Application,Domain}`; namespaces raiz `Climato.Gateway.*` / `Climato.Provider.*`; controllers em `Climato.Gateway.Api.Controllers` e `Climato.Provider.Api.Controllers`.

- [x] **Passo 1: Mover os diretórios**

```bash
cd "/c/Program Files/source/WeaterApp"
mkdir -p src/Gateway src/Provider
git mv WeatherGatewayApp/WeatherGatewayApp                src/Gateway/Climato.Gateway.Api
git mv WeatherGatewayApp/WeatherGatewayApp.Application    src/Gateway/Climato.Gateway.Application
git mv WeatherGatewayApp/WeatherGatewayApp.Domain         src/Gateway/Climato.Gateway.Domain
git mv WeatherGatewayApp/WeatherGatewayApp.Infrastructure src/Gateway/Climato.Gateway.Infrastructure
git mv WeatherProviderApp/WeatherProviderApp              src/Provider/Climato.Provider.Api
git mv WeatherProviderApp/WeatherProviderApp.Application  src/Provider/Climato.Provider.Application
git mv WeatherProviderApp/WeatherProviderApp.Domain       src/Provider/Climato.Provider.Domain
```

`WeatherProviderApp.Infrastructure`, `Scripts/`, os `.editorconfig` e `*.sln` ficam onde estão (removidos nas Tarefas 2 e 3).

- [x] **Passo 2: Renomear os `.csproj`**

```bash
cd "/c/Program Files/source/WeaterApp/src"
git mv Gateway/Climato.Gateway.Api/WeatherGatewayApp.csproj                                 Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj
git mv Gateway/Climato.Gateway.Api/WeatherGatewayApp.csproj.user                            Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj.user
git mv Gateway/Climato.Gateway.Application/WeatherGatewayApp.Application.csproj             Gateway/Climato.Gateway.Application/Climato.Gateway.Application.csproj
git mv Gateway/Climato.Gateway.Domain/WeatherGatewayApp.Domain.csproj                       Gateway/Climato.Gateway.Domain/Climato.Gateway.Domain.csproj
git mv Gateway/Climato.Gateway.Infrastructure/WeatherGatewayApp.Infrastructure.csproj       Gateway/Climato.Gateway.Infrastructure/Climato.Gateway.Infrastructure.csproj
git mv Provider/Climato.Provider.Api/WeatherProviderApp.csproj                              Provider/Climato.Provider.Api/Climato.Provider.Api.csproj
git mv Provider/Climato.Provider.Api/WeatherProviderApp.csproj.user                         Provider/Climato.Provider.Api/Climato.Provider.Api.csproj.user
git mv Provider/Climato.Provider.Application/WeatherProviderApp.Application.csproj          Provider/Climato.Provider.Application/Climato.Provider.Application.csproj
git mv Provider/Climato.Provider.Domain/WeatherProviderApp.Domain.csproj                    Provider/Climato.Provider.Domain/Climato.Provider.Domain.csproj
```

- [x] **Passo 3: Trocar namespaces e referências**

Primeiro os controllers do Gateway (que hoje usam o namespace do Provider), depois a troca global:

```bash
cd "/c/Program Files/source/WeaterApp/src"
sed -i 's/namespace WeatherProviderApp\.Controllers/namespace Climato.Gateway.Api.Controllers/' Gateway/Climato.Gateway.Api/Controllers/*.cs
sed -i 's/namespace WeatherProviderApp\.Controllers/namespace Climato.Provider.Api.Controllers/' Provider/Climato.Provider.Api/Controllers/*.cs
grep -rl --include=*.cs --include=*.csproj -E 'WeatherGatewayApp|WeatherProviderApp' . \
  | xargs sed -i -e 's/WeatherGatewayApp/Climato.Gateway/g' -e 's/WeatherProviderApp/Climato.Provider/g'
grep -rn --include=*.cs --include=*.csproj -E 'WeatherGatewayApp|WeatherProviderApp' . || echo "nenhuma ocorrência restante"
```

Esperado: `nenhuma ocorrência restante`. Conferir que os `ProjectReference` agora apontam para `..\Climato.Gateway.Domain\Climato.Gateway.Domain.csproj` etc.

- [x] **Passo 4: Criar `Climato.slnx` na raiz**

```xml
<Solution>
  <Folder Name="/Gateway/">
    <Project Path="src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj" />
    <Project Path="src/Gateway/Climato.Gateway.Application/Climato.Gateway.Application.csproj" />
    <Project Path="src/Gateway/Climato.Gateway.Domain/Climato.Gateway.Domain.csproj" />
    <Project Path="src/Gateway/Climato.Gateway.Infrastructure/Climato.Gateway.Infrastructure.csproj" />
  </Folder>
  <Folder Name="/Provider/">
    <Project Path="src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj" />
    <Project Path="src/Provider/Climato.Provider.Application/Climato.Provider.Application.csproj" />
    <Project Path="src/Provider/Climato.Provider.Domain/Climato.Provider.Domain.csproj" />
  </Folder>
</Solution>
```

O `.gitignore` atual ignora `*.sln` mas não `*.slnx`; conferir com `git check-ignore -v Climato.slnx` (esperado: sem saída).

- [x] **Passo 5: Limpar artefatos antigos e compilar**

```bash
cd "/c/Program Files/source/WeaterApp"
rm -rf src/*/*/bin src/*/*/obj
rm -f WeatherGatewayApp/WeatherGatewayApp.sln WeatherProviderApp/WeatherProviderApp.sln
dotnet build Climato.slnx
```

Esperado: `Build succeeded` (avisos de nulabilidade existentes são aceitos; ainda `net8.0`).

- [x] **Passo 6: Conferir renomeações e commitar**

```bash
git add -A src Climato.slnx WeatherGatewayApp WeatherProviderApp
git status --short | grep -v '^R' || true
```

Esperado: quase tudo `R` (renamed). Rodar `/revisar-padrao`, então:

```bash
git commit -m "Reorganiza projetos em src/ com namespaces Climato.*" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 2: .NET 10, gerenciamento central de pacotes e tooling (A12, A21 parcial, A22, A23)

**Arquivos:**
- Criar: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.config/dotnet-tools.json`
- Reescrever: `.gitignore`, os 7 `.csproj`, `launchSettings.json` e `appsettings.json` das duas Apis, `Program.cs` das duas Apis (só Swashbuckle → OpenAPI/Scalar)
- Remover: `WeatherProviderApp/` (resto), `WeatherGatewayApp/.editorconfig`, `src/*/*/*.csproj.user`

**Interfaces:**
- Produz: `UserSecretsId` `climato-gateway-api` e `climato-provider-api`; portas Gateway `7001/5001`, Provider `7002/5002`; seções `Provider:BaseUrl`, `OpenWeather:{BaseUrl,ApiKey}`, `Pexels:{BaseUrl,ApiKey}` em configuração.

- [x] **Passo 1: Migrar as chaves do Provider para user-secrets (antes de sobrescrever o `appsettings.json`)**

O `appsettings.json` local do Provider (ignorado pelo git) contém as chaves reais. Copiá-las para user-secrets **sem exibir os valores**:

```powershell
$api = "src/Provider/Climato.Provider.Api"
$json = Get-Content "$api/appsettings.json" -Raw | ConvertFrom-Json
dotnet user-secrets init --project $api --id climato-provider-api
dotnet user-secrets set "OpenWeather:ApiKey" $json.OpenWeather.ApiKey --project $api | Out-Null
dotnet user-secrets set "Pexels:ApiKey" $json.Pexels.ApiKey --project $api | Out-Null
dotnet user-secrets list --project $api | ForEach-Object { ($_ -split ' = ')[0] }
```

Esperado: lista só os nomes `OpenWeather:ApiKey` e `Pexels:ApiKey`. O `init` adiciona `<UserSecretsId>` ao `.csproj` — será sobrescrito no Passo 5 com o mesmo valor.

Para o Gateway, a connection string (com a senha do Postgres local) é definida **pelo usuário** com `dotnet user-secrets set "ConnectionStrings:Climato" "..." --id climato-gateway-api`. Só conferir que existe, sem exibir o valor:

```powershell
dotnet user-secrets list --id climato-gateway-api | ForEach-Object { ($_ -split ' = ')[0] }
```

Esperado: `ConnectionStrings:Climato`. Se não existir, reportar `NEEDS_CONTEXT`.

- [x] **Passo 2: `global.json`, `Directory.Build.props`, `Directory.Packages.props`**

`global.json`:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

`Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props` (já com os pacotes das Tarefas 3 e 4; os não usados ainda não geram erro):

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="AutoMapper" Version="14.0.0" />
    <PackageVersion Include="Mapster" Version="10.0.13" />
    <PackageVersion Include="Mapster.DependencyInjection" Version="10.0.13" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.13" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.13" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="10.0.13" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.18.0" />
  </ItemGroup>
</Project>
```

(`AutoMapper` sai na Tarefa 3.)

- [x] **Passo 3: `.editorconfig` único na raiz**

```ini
root = true

[*]
charset = utf-8
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx}]
indent_size = 4
csharp_style_namespace_declarations = file_scoped:warning

# CS8601: Possible null reference assignment.
dotnet_diagnostic.CS8601.severity = none

# CS8604: Possible null reference argument.
dotnet_diagnostic.CS8604.severity = none

# CS8613: Nullability of reference types in return type doesn't match implicitly implemented member.
dotnet_diagnostic.CS8613.severity = none

# IDE0008: Use explicit type
dotnet_diagnostic.IDE0008.severity = none

[*.{json,xml,csproj,props,slnx,yml,yaml}]
indent_size = 2

[*.{js,jsx,css,md}]
indent_size = 2
```

```bash
git rm -q WeatherGatewayApp/.editorconfig WeatherProviderApp/.editorconfig
```

- [x] **Passo 4: Reescrever o `.gitignore`**

```gitignore
# ---------- .NET ----------
**/bin/
**/obj/
**/TestResults/
**/publish/
**/*.user
**/*.suo
.vs/
**/appsettings.Development.json
**/appsettings.Local.json
**/secrets.json

# ---------- Node / Vite ----------
**/node_modules/
**/dist/
**/.vite/
**/.cache/
**/coverage/
**/.env
**/.env.local
**/.env.*.local
**/npm-debug.log*
**/yarn-debug.log*
**/yarn-error.log*

# ---------- Arquivos sensíveis ----------
**/*.pem
**/*.key
**/*.pfx
**/*.db
**/*.sqlite

# ---------- IDEs e SO ----------
**/.idea/
**/.vscode/
**/.DS_Store
**/Thumbs.db

# ---------- Claude Code (configuração pessoal) ----------
.claude/settings.local.json
.claude/worktrees/
.superpowers/
```

Conferir que passam a ser rastreáveis:

```bash
git check-ignore -v src/Gateway/Climato.Gateway.Api/appsettings.json src/Gateway/Climato.Gateway.Api/Properties/launchSettings.json Climato.slnx || echo "nenhum ignorado"
git check-ignore -v src/Gateway/Climato.Gateway.Api/appsettings.Development.json web/.env
```

Esperado: primeiro comando `nenhum ignorado`; segundo lista as duas regras.

- [x] **Passo 5: Reescrever os `.csproj`**

`src/Gateway/Climato.Gateway.Domain/Climato.Gateway.Domain.csproj` e `src/Provider/Climato.Provider.Domain/Climato.Provider.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

`src/Gateway/Climato.Gateway.Application/Climato.Gateway.Application.csproj` (a referência à Infrastructure e o Newtonsoft saem na Tarefa 3):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="AutoMapper" />
    <PackageReference Include="Newtonsoft.Json" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Gateway.Domain\Climato.Gateway.Domain.csproj" />
    <ProjectReference Include="..\Climato.Gateway.Infrastructure\Climato.Gateway.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

`src/Gateway/Climato.Gateway.Infrastructure/Climato.Gateway.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Gateway.Domain\Climato.Gateway.Domain.csproj" />
  </ItemGroup>

</Project>
```

`src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <UserSecretsId>climato-gateway-api</UserSecretsId>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Gateway.Application\Climato.Gateway.Application.csproj" />
    <ProjectReference Include="..\Climato.Gateway.Infrastructure\Climato.Gateway.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

`src/Provider/Climato.Provider.Application/Climato.Provider.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
    <PackageReference Include="Newtonsoft.Json" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Provider.Domain\Climato.Provider.Domain.csproj" />
  </ItemGroup>

</Project>
```

`src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <UserSecretsId>climato-provider-api</UserSecretsId>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Provider.Application\Climato.Provider.Application.csproj" />
  </ItemGroup>

</Project>
```

Remover `.csproj.user` e o projeto órfão:

```bash
git rm -q src/Gateway/Climato.Gateway.Api/Climato.Gateway.Api.csproj.user src/Provider/Climato.Provider.Api/Climato.Provider.Api.csproj.user
git rm -rq WeatherProviderApp
rm -rf WeatherProviderApp
```

- [x] **Passo 6: `launchSettings.json` e `appsettings.json`**

`src/Gateway/Climato.Gateway.Api/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

`src/Provider/Climato.Provider.Api/Properties/launchSettings.json`: igual, com `"applicationUrl": "https://localhost:7002;http://localhost:5002"`.

`src/Gateway/Climato.Gateway.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Provider": {
    "BaseUrl": "https://localhost:7002/"
  }
}
```

`src/Provider/Climato.Provider.Api/appsettings.json` (sobrescreve o local com as chaves — elas já estão em user-secrets pelo Passo 1):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "OpenWeather": {
    "BaseUrl": "https://api.openweathermap.org/",
    "ApiKey": ""
  },
  "Pexels": {
    "BaseUrl": "https://api.pexels.com/",
    "ApiKey": ""
  }
}
```

- [x] **Passo 7: Swashbuckle → OpenAPI nativo + Scalar nos dois `Program.cs`**

Em `src/Gateway/Climato.Gateway.Api/Program.cs` e `src/Provider/Climato.Provider.Api/Program.cs`:
- remover as linhas `builder.Services.AddEndpointsApiExplorer();`, `builder.Services.AddSwaggerGen();` e o comentário acima delas;
- adicionar `builder.Services.AddOpenApi();` logo após `builder.Services.AddControllers();`;
- trocar o bloco de desenvolvimento por:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

- adicionar `using Scalar.AspNetCore;` no topo.

- [x] **Passo 8: Manifesto de ferramentas (`dotnet-ef`)**

```bash
cd "/c/Program Files/source/WeaterApp"
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.13
```

Esperado: `.config/dotnet-tools.json` com `dotnet-ef` `10.0.13`.

- [x] **Passo 9: Namespaces file-scoped e build**

```bash
rm -rf src/*/*/bin src/*/*/obj
dotnet format Climato.slnx style --diagnostics IDE0161 --severity warn
dotnet build Climato.slnx
grep -rLn --include=*.cs -E '^namespace [A-Za-z.]+;' src | grep -v Program.cs || echo "todos file-scoped"
```

Esperado: `Build succeeded` em `net10.0`; `todos file-scoped` (o `HttpResponseMessageExtensions.cs`, sem namespace, aparece na lista — é tratado nas Tarefas 3 e 4). Se o `dotnet format` não converter algum arquivo, converter à mão.

- [x] **Passo 10: Commitar**

```bash
git add -A
git status --short
```

Conferir que **nenhum** `appsettings.Development.json`, `.env` ou segredo aparece. `/revisar-padrao`, então:

```bash
git commit -m "Atualiza para .NET 10 com gerenciamento central de pacotes" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 3: Gateway — camadas, configuração, dados e Mapster (A5, A9, A10, A11, A20, A21, A28)

**Arquivos:**
- Reescrever: `Domain/Cities/{City,ICityRepository}.cs`, `Domain/Users/{User,IUserRepository}.cs`, `Application/Contracts/*.cs`, `Application/Services/*.cs`, `Application/Model/**/*.cs`, `Infrastructure/Data/Repositories/*.cs`, `Api/Controllers/*.cs`, `Api/Program.cs`, `Application.csproj`, `Infrastructure.csproj`, `Api.csproj`
- Criar: `Application/Contracts/IWeatherProviderClient.cs`, `Application/Options/ProviderOptions.cs`, `Application/Mappings/{CityMappings,UserMappings}.cs`, `Application/DependencyInjection.cs`, `Infrastructure/Data/ClimatoDbContext.cs`, `Infrastructure/Data/Configurations/{City,User}Configuration.cs`, `Infrastructure/Http/{WeatherProviderClient,JsonDictionary,HttpResponseMessageExtensions}.cs`, `Infrastructure/DependencyInjection.cs`, `Infrastructure/Data/Migrations/*` (gerado)
- Remover: `Infrastructure/ConnectionContext.cs`, `Application/Core/` (inteira), `WeatherGatewayApp/` (resto: `Scripts/`)

Todos os caminhos abaixo são relativos a `src/Gateway/`.

**Interfaces:**
- Produz: `ICityRepository`, `IUserRepository`, `ICityAppService`, `IUserAppService`, `IWeatherAppService`, `IWeatherProviderClient` com as assinaturas abaixo (todas com `CancellationToken cancellationToken` obrigatório, exceto `DeleteAsync` dos AppServices, que muda na Tarefa 5); `services.AddGatewayApplication()`, `services.AddGatewayInfrastructure(IConfiguration)`; `ClimatoDbContext`.

- [x] **Passo 1: `.csproj` com as camadas corretas**

`Climato.Gateway.Application/Climato.Gateway.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Mapster" />
    <PackageReference Include="Mapster.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Gateway.Domain\Climato.Gateway.Domain.csproj" />
  </ItemGroup>

</Project>
```

`Climato.Gateway.Infrastructure/Climato.Gateway.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
    <PackageReference Include="Newtonsoft.Json" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Climato.Gateway.Application\Climato.Gateway.Application.csproj" />
    <ProjectReference Include="..\Climato.Gateway.Domain\Climato.Gateway.Domain.csproj" />
  </ItemGroup>

</Project>
```

`Climato.Gateway.Api/Climato.Gateway.Api.csproj`: remover a linha `<PackageReference Include="AutoMapper" />`. Em `Directory.Packages.props` (raiz), remover `<PackageVersion Include="AutoMapper" Version="14.0.0" />`.

- [x] **Passo 2: Domain sem anotações de persistência e com `CancellationToken`**

`Climato.Gateway.Domain/Cities/City.cs`:

```csharp
namespace Climato.Gateway.Domain.Cities;

public class City
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }

    public City() { }

    public City(string name, string country, Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name ?? string.Empty;
        Country = country ?? string.Empty;
        IsFavorite = false;
        CreatedAt = DateTime.UtcNow;
    }
}
```

`Climato.Gateway.Domain/Cities/ICityRepository.cs`:

```csharp
namespace Climato.Gateway.Domain.Cities;

public interface ICityRepository
{
    Task<City?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<List<City>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<City>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken);
    Task<City?> GetByNameAsync(string cityName, Guid userId, CancellationToken cancellationToken);
    Task CreateAsync(City city, CancellationToken cancellationToken);
    Task UpdateAsync(City city, CancellationToken cancellationToken);
    Task DeleteAsync(City city, CancellationToken cancellationToken);
}
```

`Climato.Gateway.Domain/Users/User.cs`:

```csharp
namespace Climato.Gateway.Domain.Users;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User() { }
}
```

`Climato.Gateway.Domain/Users/IUserRepository.cs`:

```csharp
namespace Climato.Gateway.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task CreateAsync(User user, CancellationToken cancellationToken);
    Task DeleteAsync(User user, CancellationToken cancellationToken);
    Task<bool> AllowCreateAsync(string email, CancellationToken cancellationToken);
}
```

`Climato.Gateway.Domain/Weathers/Weather.cs` não muda.

- [x] **Passo 3: Application — DTOs, contratos, Options**

Mover os DTOs para o namespace igual à pasta:

`Climato.Gateway.Application/Model/Cities/CityDto.cs`:

```csharp
namespace Climato.Gateway.Application.Model.Cities;

public class CityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

`Climato.Gateway.Application/Model/Users/UserDto.cs` (`Password` permanece até a Fase 02 — A3):

```csharp
namespace Climato.Gateway.Application.Model.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

`Climato.Gateway.Application/Model/Users/UserGetDto.cs`:

```csharp
namespace Climato.Gateway.Application.Model.Users;

public class UserGetDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

`Climato.Gateway.Application/Model/Users/UserPostDto.cs`:

```csharp
namespace Climato.Gateway.Application.Model.Users;

public class UserPostDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

`Climato.Gateway.Application/Contracts/ICityAppService.cs` (`DeleteAsync` muda na Tarefa 5):

```csharp
using Climato.Gateway.Application.Model.Cities;

namespace Climato.Gateway.Application.Contracts;

public interface ICityAppService
{
    Task<List<CityDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<CityDto>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken);
    Task<CityDto?> ToggleFavoriteAsync(Guid id, CancellationToken cancellationToken);
    void DeleteAsync(Guid id);
}
```

`Climato.Gateway.Application/Contracts/IUserAppService.cs`:

```csharp
using Climato.Gateway.Application.Model.Users;

namespace Climato.Gateway.Application.Contracts;

public interface IUserAppService
{
    Task<UserDto?> PostAsync(UserPostDto dto, CancellationToken cancellationToken);
    Task<UserDto?> GetAsync(string email, string password, CancellationToken cancellationToken);
    void DeleteAsync(Guid id);
}
```

`Climato.Gateway.Application/Contracts/IWeatherAppService.cs`:

```csharp
using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherAppService
{
    Task<Weather> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken);
}
```

`Climato.Gateway.Application/Contracts/IWeatherProviderClient.cs`:

```csharp
using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Application.Contracts;

public interface IWeatherProviderClient
{
    Task<Weather> GetWeatherAsync(string cityName, CancellationToken cancellationToken);
}
```

`Climato.Gateway.Application/Options/ProviderOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Application.Options;

public sealed class ProviderOptions
{
    public const string SectionName = "Provider";

    [Required(ErrorMessage = "Provider:BaseUrl não configurada. Ver docs/runbook.md.")]
    [Url(ErrorMessage = "Provider:BaseUrl deve ser uma URL absoluta.")]
    public string BaseUrl { get; init; } = string.Empty;
}
```

- [x] **Passo 4: Application — Mapster e serviços**

```bash
git rm -rq src/Gateway/Climato.Gateway.Application/Core
```

`Climato.Gateway.Application/Mappings/CityMappings.cs`:

```csharp
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Domain.Cities;
using Mapster;

namespace Climato.Gateway.Application.Mappings;

public class CityMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<City, CityDto>();
    }
}
```

`Climato.Gateway.Application/Mappings/UserMappings.cs`:

```csharp
using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Domain.Users;
using Mapster;

namespace Climato.Gateway.Application.Mappings;

public class UserMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>();
    }
}
```

`Climato.Gateway.Application/Services/CityAppService.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Domain.Cities;
using MapsterMapper;

namespace Climato.Gateway.Application.Services;

public class CityAppService : ICityAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IMapper mapper;

    public CityAppService(ICityRepository repository, IMapper mapper)
    {
        cityRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async void DeleteAsync(Guid id)
    {
        var city = await cityRepository.GetAsync(id, CancellationToken.None) ?? throw new ArgumentException("Cidade não encontrada");
        await cityRepository.DeleteAsync(city, CancellationToken.None);
    }

    public async Task<List<CityDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cities = await cityRepository.GetAllAsync(userId, cancellationToken);
        return mapper.Map<List<CityDto>>(cities);
    }

    public async Task<List<CityDto>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cities = await cityRepository.GetAllFavoritesAsync(userId, cancellationToken);
        return mapper.Map<List<CityDto>>(cities);
    }

    public async Task<CityDto?> ToggleFavoriteAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, cancellationToken);

        if (city == null)
        {
            return null;
        }

        city.IsFavorite = !city.IsFavorite;
        await cityRepository.UpdateAsync(city, cancellationToken);

        return mapper.Map<CityDto>(city);
    }
}
```

(O `async void` permanece só até a Tarefa 5, que corrige o A8 em commit próprio.)

`Climato.Gateway.Application/Services/UserAppService.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Domain.Users;
using MapsterMapper;

namespace Climato.Gateway.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UserAppService(IUserRepository repository, IMapper mapper)
    {
        userRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async void DeleteAsync(Guid id)
    {
        var user = await userRepository.GetAsync(id, CancellationToken.None) ?? throw new ArgumentException("Usuário não encontrado");
        await userRepository.DeleteAsync(user, CancellationToken.None);
    }

    public async Task<UserDto?> GetAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(email, cancellationToken);

        if (user == null || user.Password != password)
        {
            return null;
        }

        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> PostAsync(UserPostDto dto, CancellationToken cancellationToken)
    {
        var allowCreate = await userRepository.AllowCreateAsync(dto.Email, cancellationToken);

        if (!allowCreate)
        {
            return null;
        }

        var user = new User()
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Name = dto.Name,
            Password = dto.Password,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(user, cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}
```

`Climato.Gateway.Application/Services/WeatherAppService.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly ICityRepository cityRepository;
    private readonly IWeatherProviderClient weatherProviderClient;

    public WeatherAppService(ICityRepository repository, IWeatherProviderClient providerClient)
    {
        cityRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        weatherProviderClient = providerClient ?? throw new ArgumentNullException(nameof(providerClient));
    }

    public async Task<Weather> GetByCityAsync(string cityName, Guid userId, CancellationToken cancellationToken)
    {
        var weather = await weatherProviderClient.GetWeatherAsync(cityName, cancellationToken);
        var city = await cityRepository.GetByNameAsync(weather.City, userId, cancellationToken);

        if (city == null)
        {
            var newCity = new City(weather.City, weather.Country, userId);
            await cityRepository.CreateAsync(newCity, cancellationToken);
        }

        return weather;
    }
}
```

`Climato.Gateway.Application/DependencyInjection.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Climato.Gateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayApplication(this IServiceCollection services)
    {
        var mappingConfig = TypeAdapterConfig.GlobalSettings;
        mappingConfig.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(mappingConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<ICityAppService, CityAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IWeatherAppService, WeatherAppService>();

        return services;
    }
}
```

- [x] **Passo 5: Infrastructure — DbContext e configurations**

```bash
git rm -q src/Gateway/Climato.Gateway.Infrastructure/ConnectionContext.cs
```

`Climato.Gateway.Infrastructure/Data/ClimatoDbContext.cs`:

```csharp
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Climato.Gateway.Infrastructure.Data;

public class ClimatoDbContext : DbContext
{
    public ClimatoDbContext(DbContextOptions<ClimatoDbContext> options) : base(options) { }

    public DbSet<City> Cities => Set<City>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClimatoDbContext).Assembly);
    }
}
```

`Climato.Gateway.Infrastructure/Data/Configurations/UserConfiguration.cs`:

```csharp
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Climato.Gateway.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
        builder.Property(e => e.CreatedAt)
            .HasColumnName("createdat")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_user_email");
    }
}
```

`Climato.Gateway.Infrastructure/Data/Configurations/CityConfiguration.cs`:

```csharp
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Climato.Gateway.Infrastructure.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("city");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("userid");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Country).HasColumnName("country").HasMaxLength(255).IsRequired();
        builder.Property(e => e.IsFavorite).HasColumnName("isfavorite");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("createdat")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_city_user");

        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_city_userid");
    }
}
```

- [x] **Passo 6: Infrastructure — repositórios via DI**

`Climato.Gateway.Infrastructure/Data/Repositories/CityRepository.cs`:

```csharp
using Climato.Gateway.Domain.Cities;
using Microsoft.EntityFrameworkCore;

namespace Climato.Gateway.Infrastructure.Data.Repositories;

public class CityRepository : ICityRepository
{
    private readonly ClimatoDbContext context;

    public CityRepository(ClimatoDbContext context)
    {
        this.context = context;
    }

    public async Task<City?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(City city, CancellationToken cancellationToken)
    {
        await context.Cities.AddAsync(city, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(City city, CancellationToken cancellationToken)
    {
        context.Cities.Update(city);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<City>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<City>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.UserId == userId && c.IsFavorite)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<City?> GetByNameAsync(string cityName, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.Name == cityName && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAsync(City city, CancellationToken cancellationToken)
    {
        context.Cities.Remove(city);
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

`Climato.Gateway.Infrastructure/Data/Repositories/UserRepository.cs`:

```csharp
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Climato.Gateway.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ClimatoDbContext context;

    public UserRepository(ClimatoDbContext context)
    {
        this.context = context;
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetAsync(string email, CancellationToken cancellationToken)
    {
        return await context.Users
            .Where(c => c.Email == email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Users
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AllowCreateAsync(string email, CancellationToken cancellationToken)
    {
        var exists = await context.Users.AnyAsync(c => c.Email == email, cancellationToken);
        return !exists;
    }
}
```

- [x] **Passo 7: Infrastructure — client HTTP do Provider**

O parsing atual vai para a Infrastructure **sem mudança de lógica** (a Fase 03 substitui por DTO tipado — A19).

`Climato.Gateway.Infrastructure/Http/JsonDictionary.cs`: conteúdo idêntico ao antigo `Application/Core/Collections/JsonDictionary.cs` (lido no `git show HEAD:src/Gateway/Climato.Gateway.Application/Core/Collections/JsonDictionary.cs`), trocando só a linha de namespace por:

```csharp
namespace Climato.Gateway.Infrastructure.Http;
```

e convertendo para file-scoped (o corpo da classe não muda).

`Climato.Gateway.Infrastructure/Http/HttpResponseMessageExtensions.cs`:

```csharp
namespace Climato.Gateway.Infrastructure.Http;

public static class HttpResponseMessageExtensions
{
    public static async Task<JsonDictionary> ReadContentAsDictionaryAsync(this HttpResponseMessage message, CancellationToken cancellationToken)
    {
        var json = await message.Content.ReadAsStringAsync(cancellationToken);
        return new JsonDictionary(json);
    }
}
```

`Climato.Gateway.Infrastructure/Http/WeatherProviderClient.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Domain.Weathers;

namespace Climato.Gateway.Infrastructure.Http;

public class WeatherProviderClient : IWeatherProviderClient
{
    private readonly HttpClient client;

    public WeatherProviderClient(HttpClient httpClient)
    {
        client = httpClient;
    }

    public async Task<Weather> GetWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"weathers?cityName={Uri.EscapeDataString(cityName)}", cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.ReadContentAsDictionaryAsync(cancellationToken);

        return new Weather
        {
            City = result["city"]?.ToString(),
            Country = result["country"]?.ToString(),
            Description = result["description"]?.ToString(),
            Icon = result["icon"]?.ToString(),
            Temperature = Convert.ToDouble(result["temperature"]),
            FeelsLike = Convert.ToDouble(result["feelsLike"]),
            TempMin = Convert.ToDouble(result["tempMin"]),
            TempMax = Convert.ToDouble(result["tempMax"]),
            Humidity = Convert.ToInt32(result["humidity"]),
            Pressure = Convert.ToInt32(result["pressure"]),
            WindSpeed = Convert.ToDouble(result["windSpeed"]),
            WindDirection = Convert.ToInt32(result["windDirection"]),
            Sunrise = DateTime.Parse(result["sunrise"]?.ToString() ?? DateTime.MinValue.ToString()),
            Sunset = DateTime.Parse(result["sunset"]?.ToString() ?? DateTime.MinValue.ToString()),
            ImageUrl = result["imageUrl"]?.ToString()
        };
    }
}
```

`Climato.Gateway.Infrastructure/DependencyInjection.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Options;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Climato.Gateway.Infrastructure.Data;
using Climato.Gateway.Infrastructure.Data.Repositories;
using Climato.Gateway.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Climato.Gateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Climato");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Climato não configurada. Ver docs/runbook.md.");
        }

        services.AddDbContext<ClimatoDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddOptions<ProviderOptions>()
            .BindConfiguration(ProviderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IWeatherProviderClient, WeatherProviderClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }
}
```

- [x] **Passo 8: Api — controllers e `Program.cs`**

`Climato.Gateway.Api/Controllers/CityController.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("cities")]
public class CityController : ControllerBase
{
    private readonly ICityAppService appService;

    public CityController(ICityAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna todas as cidades cadastradas no sistema.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CityDto>>> GetAll(Guid userId, CancellationToken cancellationToken)
    {
        var result = await appService.GetAllAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna todas as cidades favoritas no sistema.</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<List<CityDto>>> GetAllFavorites(Guid userId, CancellationToken cancellationToken)
    {
        var result = await appService.GetAllFavoritesAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Favorita/Desfavorita a cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    [HttpPatch("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id, CancellationToken cancellationToken)
    {
        var city = await appService.ToggleFavoriteAsync(id, cancellationToken);

        if (city == null)
        {
            return NotFound();
        }

        return Ok(city);
    }

    /// <summary>Remover cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        appService.DeleteAsync(id);
        return Ok();
    }
}
```

`Climato.Gateway.Api/Controllers/UserController.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Users;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IUserAppService appService;

    public UserController(IUserAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Cria um usuário.</summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Post(UserPostDto dto, CancellationToken cancellationToken)
    {
        var user = await appService.PostAsync(dto, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>Busca o usuário por e-mail e senha.</summary>
    [HttpGet]
    public async Task<ActionResult<UserDto>> Get([FromQuery] UserGetDto dto, CancellationToken cancellationToken)
    {
        var user = await appService.GetAsync(dto.Email, dto.Password, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>Remove o usuário informado.</summary>
    /// <param name="id">Id do usuário</param>
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        appService.DeleteAsync(id);
        return Ok();
    }
}
```

`Climato.Gateway.Api/Controllers/WeatherController.cs`:

```csharp
using Climato.Gateway.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("weathers")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherAppService appService;

    public WeatherController(IWeatherAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna os dados climáticos da cidade informada.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="userId">Id do usuário que pesquisou</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    [HttpGet("{cityName}")]
    public async Task<IActionResult> GetWeather(string cityName, [FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        var weather = await appService.GetByCityAsync(cityName, userId, cancellationToken);

        if (weather == null)
        {
            return NotFound();
        }

        return Ok(weather);
    }
}
```

`Climato.Gateway.Api/Program.cs`:

```csharp
using Climato.Gateway.Application;
using Climato.Gateway.Infrastructure;
using Climato.Gateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddGatewayApplication()
    .AddGatewayInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ClimatoDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();
```

- [x] **Passo 9: Build**

```bash
cd "/c/Program Files/source/WeaterApp"
dotnet build Climato.slnx
grep -rn --include=*.cs -E 'AutoMapper|new ClimatoDbContext|new ConnectionContext|localhost:44313' src || echo "limpo"
```

Esperado: `Build succeeded`; `limpo`.

- [x] **Passo 10: Migration `Inicial`**

```bash
dotnet tool restore
dotnet ef migrations add Inicial \
  --project src/Gateway/Climato.Gateway.Infrastructure \
  --startup-project src/Gateway/Climato.Gateway.Api \
  --output-dir Data/Migrations
dotnet ef migrations script \
  --project src/Gateway/Climato.Gateway.Infrastructure \
  --startup-project src/Gateway/Climato.Gateway.Api
```

Esperado no SQL: `CREATE TABLE "user"` **antes** de `CREATE TABLE city`; `createdat timestamp with time zone NOT NULL DEFAULT (now())`; `CONSTRAINT fk_city_user FOREIGN KEY (userid) REFERENCES "user" (id) ON DELETE CASCADE`; `CREATE INDEX ix_city_userid`; `CREATE UNIQUE INDEX ix_user_email`. Se divergir, corrigir as configurations, `dotnet ef migrations remove` e gerar de novo.

- [x] **Passo 11: Remover o script SQL antigo e commitar**

```bash
git rm -rq WeatherGatewayApp
rm -rf WeatherGatewayApp
dotnet build Climato.slnx
git add -A
```

`/revisar-padrao`, então:

```bash
git commit -m "Corrige camadas, configuração e acesso a dados do Gateway" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 4: Provider — Options, typed clients e `CancellationToken` (A20, A21)

Caminhos relativos a `src/Provider/`.

**Arquivos:**
- Criar: `Climato.Provider.Application/Options/{OpenWeatherOptions,PexelsOptions}.cs`, `Climato.Provider.Application/DependencyInjection.cs`
- Reescrever: `Application/Contracts/*.cs`, `Application/Services/*.cs`, `Application/Core/Collections/HttpResponseMessageExtensions.cs`, `Api/Controllers/WeatherController.cs`, `Api/Program.cs`

**Interfaces:**
- Produz: `IWeatherAppService.GetByCityAsync(string cityName, CancellationToken cancellationToken)`, `ICityImageAppService.GetImageUrlAsync(string? cityName, CancellationToken cancellationToken)`, `services.AddProviderApplication()`.

- [x] **Passo 1: Options**

`Climato.Provider.Application/Options/OpenWeatherOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Climato.Provider.Application.Options;

public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";

    [Required(ErrorMessage = "OpenWeather:BaseUrl não configurada. Ver docs/runbook.md.")]
    [Url(ErrorMessage = "OpenWeather:BaseUrl deve ser uma URL absoluta.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "OpenWeather:ApiKey não configurada. Ver docs/runbook.md.")]
    public string ApiKey { get; init; } = string.Empty;
}
```

`Climato.Provider.Application/Options/PexelsOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Climato.Provider.Application.Options;

public sealed class PexelsOptions
{
    public const string SectionName = "Pexels";

    [Required(ErrorMessage = "Pexels:BaseUrl não configurada. Ver docs/runbook.md.")]
    [Url(ErrorMessage = "Pexels:BaseUrl deve ser uma URL absoluta.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "Pexels:ApiKey não configurada. Ver docs/runbook.md.")]
    public string ApiKey { get; init; } = string.Empty;
}
```

- [x] **Passo 2: Contratos e extensão com `CancellationToken`**

`Climato.Provider.Application/Contracts/IWeatherAppService.cs`:

```csharp
using Climato.Provider.Domain.Weathers;

namespace Climato.Provider.Application.Contracts;

public interface IWeatherAppService
{
    Task<Weather> GetByCityAsync(string cityName, CancellationToken cancellationToken);
}
```

`Climato.Provider.Application/Contracts/ICityImageAppService.cs`:

```csharp
namespace Climato.Provider.Application.Contracts;

public interface ICityImageAppService
{
    Task<string?> GetImageUrlAsync(string? cityName, CancellationToken cancellationToken);
}
```

`Climato.Provider.Application/Core/Collections/HttpResponseMessageExtensions.cs`:

```csharp
namespace Climato.Provider.Application.Core.Collections;

public static class HttpResponseMessageExtensions
{
    public static async Task<JsonDictionary> ReadContentAsDictionaryAsync(this HttpResponseMessage message, CancellationToken cancellationToken)
    {
        var json = await message.Content.ReadAsStringAsync(cancellationToken);
        return new JsonDictionary(json);
    }
}
```

- [x] **Passo 3: Serviços com Options e caminhos relativos**

`Climato.Provider.Application/Services/CityImageAppService.cs`:

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Core.Collections;
using Climato.Provider.Application.Options;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application.Services;

public class CityImageAppService : ICityImageAppService
{
    private readonly HttpClient client;
    private readonly string apiKey;

    public CityImageAppService(HttpClient httpClient, IOptions<PexelsOptions> options)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
    }

    public async Task<string?> GetImageUrlAsync(string? cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"v1/search?query={Uri.EscapeDataString(cityName)}&orientation=landscape&per_page=1");
        request.Headers.Add("Authorization", apiKey);

        var response = await client.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.ReadContentAsDictionaryAsync(cancellationToken);
        string? cityImage = null;

        if (result.TryGetValue("photos", out var photosObj) && photosObj is List<object> photos && photos.Count > 0)
        {
            if (photos[0] is Dictionary<string, object> photo &&
                photo.TryGetValue("src", out var srcObj) &&
                srcObj is Dictionary<string, object> src &&
                src.TryGetValue("large2x", out var imageUrlObj))
            {
                cityImage = imageUrlObj?.ToString();
            }
        }

        return cityImage;
    }
}
```

`Climato.Provider.Application/Services/WeatherAppService.cs`:

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Core.Collections;
using Climato.Provider.Application.Options;
using Climato.Provider.Domain.Weathers;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application.Services;

public class WeatherAppService : IWeatherAppService
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly ICityImageAppService cityImageAppService;

    public WeatherAppService(HttpClient httpClient, IOptions<OpenWeatherOptions> options, ICityImageAppService cityImageAppService)
    {
        client = httpClient;
        apiKey = options.Value.ApiKey;
        this.cityImageAppService = cityImageAppService;
    }

    public async Task<Weather> GetByCityAsync(string cityName, CancellationToken cancellationToken)
    {
        var url = $"data/2.5/weather?q={Uri.EscapeDataString(cityName)}&appid={apiKey}&units=metric&lang=pt_br";
        var response = await client.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.ReadContentAsDictionaryAsync(cancellationToken);
        var weather = new Weather
        {
            City = result["name"]?.ToString(),
            Country = ((Dictionary<string, object>)result["sys"])["country"]?.ToString(),
            Description = ((List<object>)result["weather"])[0] is Dictionary<string, object> weatherDict ? weatherDict["description"]?.ToString() : null,
            Icon = ((List<object>)result["weather"])[0] is Dictionary<string, object> weatherIconDict ? weatherIconDict["icon"]?.ToString() : null,
            Temperature = Convert.ToDouble(((Dictionary<string, object>)result["main"])["temp"]),
            FeelsLike = Convert.ToDouble(((Dictionary<string, object>)result["main"])["feels_like"]),
            TempMin = Convert.ToDouble(((Dictionary<string, object>)result["main"])["temp_min"]),
            TempMax = Convert.ToDouble(((Dictionary<string, object>)result["main"])["temp_max"]),
            Humidity = Convert.ToInt32(((Dictionary<string, object>)result["main"])["humidity"]),
            Pressure = Convert.ToInt32(((Dictionary<string, object>)result["main"])["pressure"]),
            WindSpeed = Convert.ToDouble(((Dictionary<string, object>)result["wind"])["speed"]),
            WindDirection = Convert.ToInt32(((Dictionary<string, object>)result["wind"])["deg"]),
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(((Dictionary<string, object>)result["sys"])["sunrise"])).DateTime,
            Sunset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(((Dictionary<string, object>)result["sys"])["sunset"])).DateTime,
        };

        weather.ImageUrl = await cityImageAppService.GetImageUrlAsync(weather.City, cancellationToken);

        return weather;
    }
}
```

- [x] **Passo 4: DI da Application**

`Climato.Provider.Application/DependencyInjection.cs`:

```csharp
using Climato.Provider.Application.Contracts;
using Climato.Provider.Application.Options;
using Climato.Provider.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Climato.Provider.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProviderApplication(this IServiceCollection services)
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
        });

        services.AddHttpClient<ICityImageAppService, CityImageAppService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PexelsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }
}
```

- [x] **Passo 5: Api — controller e `Program.cs`**

`Climato.Provider.Api/Controllers/WeatherController.cs`:

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
    [HttpGet]
    public async Task<ActionResult<Weather>> Get([FromQuery] string cityName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return BadRequest("Parâmetro 'cityName' é obrigatório.");
        }

        try
        {
            var result = await appService.GetByCityAsync(cityName, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException)
        {
            return NotFound($"Não foi possível obter dados para a cidade '{cityName}'.");
        }
    }
}
```

`Climato.Provider.Api/Program.cs`:

```csharp
using Climato.Provider.Application;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddProviderApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
```

- [x] **Passo 6: Build e commit**

```bash
cd "/c/Program Files/source/WeaterApp"
dotnet build Climato.slnx
grep -rn --include=*.cs -E 'AddTransient|config\["' src/Provider || echo "limpo"
git add -A
```

Esperado: `Build succeeded`; `limpo`. `/revisar-padrao`, então:

```bash
git commit -m "Configura Options e typed clients no Provider" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 5: Exclusões assíncronas (A8) e URL da API no front (A27 parcial)

**Arquivos:**
- Modificar: `src/Gateway/Climato.Gateway.Application/Contracts/{ICityAppService,IUserAppService}.cs`, `src/Gateway/Climato.Gateway.Application/Services/{CityAppService,UserAppService}.cs`, `src/Gateway/Climato.Gateway.Api/Controllers/{CityController,UserController}.cs`, `web/src/services/api.js`
- Criar: `web/.env.example`, `web/.env` (local, ignorado)

**Interfaces:**
- Consome: assinaturas da Tarefa 3.
- Produz: `Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)` em `ICityAppService` e `IUserAppService`; `DELETE /cities/{id}` e `DELETE /users/{id}` → `204` ou `404`.

- [x] **Passo 1: Contratos**

Em `ICityAppService.cs` e `IUserAppService.cs`, trocar `void DeleteAsync(Guid id);` por:

```csharp
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
```

- [x] **Passo 2: AppServices**

Em `CityAppService.cs`, substituir o método `DeleteAsync` por:

```csharp
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, cancellationToken);

        if (city == null)
        {
            return false;
        }

        await cityRepository.DeleteAsync(city, cancellationToken);
        return true;
    }
```

Em `UserAppService.cs`, substituir o método `DeleteAsync` por:

```csharp
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(id, cancellationToken);

        if (user == null)
        {
            return false;
        }

        await userRepository.DeleteAsync(user, cancellationToken);
        return true;
    }
```

- [x] **Passo 3: Controllers**

Em `CityController.cs`, substituir a action `Delete` por:

```csharp
    /// <summary>Remove a cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Cidade removida.</response>
    /// <response code="404">Cidade não encontrada.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
```

Em `UserController.cs`, substituir a action `Delete` por:

```csharp
    /// <summary>Remove o usuário informado.</summary>
    /// <param name="id">Id do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Usuário removido.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
```

- [x] **Passo 4: Build e verificação de `async void`**

```bash
dotnet build Climato.slnx
grep -rn --include=*.cs 'async void' src || echo "nenhum async void"
```

Esperado: `Build succeeded`; `nenhum async void`.

- [x] **Passo 5: Frontend — `VITE_API_URL`**

Em `web/src/services/api.js`, trocar a linha 3:

```js
const API = "https://localhost:44394";
```

por:

```js
const API = import.meta.env.VITE_API_URL;
```

Criar `web/.env.example`:

```
VITE_API_URL=https://localhost:7001
```

Criar `web/.env` local com o mesmo conteúdo (ignorado — conferir com `git check-ignore -v web/.env`).

```bash
npm --prefix web run lint
npm --prefix web run build
```

Esperado: lint sem erros novos; build gera `web/dist/`.

- [x] **Passo 6: Commit**

```bash
git add -A
git status --short
```

Conferir que `web/.env` **não** aparece. `/revisar-padrao`, então:

```bash
git commit -m "Corrige exclusões assíncronas e lê a URL da API do ambiente" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 6: Documentação da Fase 01

**Arquivos:** `docs/runbook.md`, `CLAUDE.md`, `llms.txt`, `docs/architecture/{backend,system-design,review-checklist}.md`, `docs/architecture/decisions/{README,0005-mapster}.md`, `docs/features/00-roteiro-portfolio/auditoria-inicial.md`, `docs/project-overview.md`, `.claude/skills/revisar-padrao/SKILL.md`, `README.md`.

- [x] **Passo 1: `docs/runbook.md` (reescrever)**

````markdown
# Runbook — Ambiente local

Alvo futuro: `docker compose up` (Fase 05). Até lá, os serviços sobem com `dotnet run`.

## Pré-requisitos

- .NET SDK 10 (versão fixada em `global.json`)
- Node.js LTS (≥ 20)
- PostgreSQL rodando em `localhost:5432`
- Certificado HTTPS de desenvolvimento confiável: `dotnet dev-certs https --trust`
- Chaves gratuitas da [OpenWeather](https://openweathermap.org/api) e da [Pexels](https://www.pexels.com/api/)

## 1. Ferramentas

```bash
dotnet tool restore
```

Instala o `dotnet-ef` na versão do `.config/dotnet-tools.json`.

## 2. Segredos (user-secrets)

Nada de segredo vai para o repositório. Configurar uma vez por máquina:

```bash
dotnet user-secrets set "ConnectionStrings:Climato" "Host=localhost;Port=5432;Database=climato;Username=postgres;Password=<sua senha>" --project src/Gateway/Climato.Gateway.Api
dotnet user-secrets set "OpenWeather:ApiKey" "<sua chave>" --project src/Provider/Climato.Provider.Api
dotnet user-secrets set "Pexels:ApiKey" "<sua chave>" --project src/Provider/Climato.Provider.Api
```

Se faltar alguma, a API não sobe e informa qual chave está ausente.

Frontend: copiar `web/.env.example` para `web/.env`.

## 3. Banco de dados

O banco `climato` é criado e migrado **automaticamente** quando o Gateway sobe fora de `Production`. Para aplicar manualmente:

```bash
dotnet ef database update --project src/Gateway/Climato.Gateway.Infrastructure --startup-project src/Gateway/Climato.Gateway.Api
```

### Criando uma migration

Mudou entidade ou configuration → nova migration no mesmo PR:

```bash
dotnet ef migrations add <NomeEmPascalCase> --project src/Gateway/Climato.Gateway.Infrastructure --startup-project src/Gateway/Climato.Gateway.Api --output-dir Data/Migrations
```

> Bancos `weather-app` criados antes da Fase 01 (script SQL) não são usados mais — podem ser apagados.

## 4. Subir a aplicação

Em três terminais:

```bash
dotnet run --project src/Provider/Climato.Provider.Api --launch-profile https
dotnet run --project src/Gateway/Climato.Gateway.Api --launch-profile https
npm --prefix web install && npm --prefix web run dev
```

| Serviço | URL | Documentação |
|---|---|---|
| Provider | `https://localhost:7002` | `https://localhost:7002/scalar` |
| Gateway | `https://localhost:7001` | `https://localhost:7001/scalar` |
| Web | `http://localhost:5173` | — |

Visual Studio / Rider: abrir `Climato.slnx` e usar o perfil `https` de cada Api.

## 5. Problemas comuns

| Sintoma | Causa |
|---|---|
| `OptionsValidationException: OpenWeather:ApiKey não configurada` | Faltou o user-secret do Provider (seção 2) |
| `ConnectionStrings:Climato não configurada` | Faltou o user-secret do Gateway |
| Toda busca mostra "cidade não encontrada" | Provider fora do ar ou chave inválida — erros ainda viram 404/500 indistintos (A14, A15, Fase 03) |
| `ERR_CERT_AUTHORITY_INVALID` no navegador | `dotnet dev-certs https --trust` e abrir `https://localhost:7001/scalar` uma vez |

## 6. Verificação antes de PR

Comandos na seção [Verificação do CLAUDE.md](../CLAUDE.md#verificação).
````

- [x] **Passo 2: `CLAUDE.md`**

Na tabela "Documentos de Referência", trocar `` `WeatherGatewayApp/` ou `WeatherProviderApp/` `` por `` `src/Gateway/` ou `src/Provider/` ``. Substituir a seção **Verificação** por:

````markdown
### Verificação

```bash
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

Cada fase que mudar isso (`dotnet test` na 04, `docker compose` na 05) atualiza esta seção e o [`runbook.md`](docs/runbook.md).
````

- [x] **Passo 3: `llms.txt`**

Trocar as linhas de serviços por:

```
- `src/Provider/` — .NET 10; integra OpenWeather + Pexels; sem estado (cache a partir da Fase 03).
- `src/Gateway/` — .NET 10 + EF Core + PostgreSQL; usuários, cidades, favoritos; único ponto de entrada do frontend.
- `web/` — React 19 + Vite + Ant Design.
```

- [x] **Passo 4: `docs/architecture/backend.md`**

- Tabela **Stack**: coluna "Atual" passa a ser o estado pós-Fase 01 — Runtime `.NET 10`; Persistência `EF Core 10 + Npgsql via AddDbContext, Migrations`; Mapeamento `Mapster`; documentação `OpenAPI nativo + Scalar`. Remover as entradas de "Alvo" já atingidas.
- Seção **Estrutura da Solução**: substituir a árvore por:

```
Climato.slnx                         ← solução única; Directory.Build.props / Directory.Packages.props / global.json na raiz
src/Gateway/
├── Climato.Gateway.Api/             ← Program.cs, Controllers
├── Climato.Gateway.Application/     ← Contracts, Services, Model (DTOs), Mappings (Mapster), Options, DependencyInjection.cs
├── Climato.Gateway.Domain/          ← Entidades e interfaces de repositório por agregado (Cities/, Users/, Weathers/)
└── Climato.Gateway.Infrastructure/  ← Data (ClimatoDbContext, Configurations, Migrations, Repositories), Http (clients), DependencyInjection.cs
src/Provider/
├── Climato.Provider.Api/
├── Climato.Provider.Application/    ← Clientes das APIs externas (Services), Options, DependencyInjection.cs
└── Climato.Provider.Domain/         ← Weather (contrato devolvido ao Gateway)
```

  e remover o parágrafo "**Alvo (Fase 01):**".
- Seção **Dependências entre camadas**: acrescentar "Versões de pacote só em `Directory.Packages.props`; `.csproj` sem `Version`."
- Seção **Repositórios**: trocar "Recebem o `ConnectionContext` pelo construtor (**a partir da Fase 01**; hoje usam `new` — A10)." por "Recebem o `ClimatoDbContext` pelo construtor e são registrados como `Scoped`. Todo método recebe `CancellationToken`."
- Nova subseção em **Convenções de Código**, após "DTOs (Application)":

```markdown
### Mapeamento (Mapster)
- Configuração em `Application/Mappings/` com uma classe `IRegister` por agregado (`CityMappings`, `UserMappings`).
- AppServices recebem `MapsterMapper.IMapper` e chamam `mapper.Map<TDestino>(origem)`. Nada de `.Adapt()` ou `TypeAdapterConfig` fora de `Mappings/` e do `DependencyInjection.cs`.
- Decisão: [ADR 0005](decisions/0005-mapster.md).
```

- Nova subseção em **Convenções de Código**, após "Configuração e segredos":

```markdown
### Injeção de dependência por camada
- Cada projeto expõe `DependencyInjection.cs` com uma extensão `Add<Servico><Camada>()` (`AddGatewayApplication`, `AddGatewayInfrastructure(configuration)`, `AddProviderApplication`).
- `Program.cs` só chama essas extensões, configura o pipeline e (no Gateway) aplica migrations fora de `Production`.
- Options: classe `*Options` com `const string SectionName`, registrada com `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`; mensagens de `[Required]` em português citando a chave.
```

- Seção **Migrations**: remover "**(a partir da Fase 01)**" do título e substituir o texto por: "EF Core Migrations em `Gateway.Infrastructure/Data/Migrations/`. Mudança de entidade ou configuration → nova migration no mesmo PR. Aplicadas automaticamente no startup fora de `Production`; comandos no [`runbook.md`](../runbook.md#3-banco-de-dados)."
- Em **Geral**, trocar "`CancellationToken` propagado do controller ao repositório e ao `HttpClient` (a partir da Fase 01)." por "`CancellationToken` obrigatório (sem default) nas interfaces e propagado do controller ao repositório e ao `HttpClient`."

- [x] **Passo 5: `docs/architecture/system-design.md`**

No diagrama "Estado atual", trocar `gw -- "HTTP (URL fixa)" --> pv["WeatherProviderApp"]` por `gw -- "HTTP (Provider:BaseUrl)" --> pv["Climato.Provider"]`, `subgraph Gateway["WeatherGatewayApp"]` por `subgraph Gateway["Climato.Gateway"]`. No alvo, trocar os nomes dos subgraphs da mesma forma. Na seção **Dados**, trocar "Schema versionado por EF Core Migrations a partir da Fase 01." por "Schema versionado por EF Core Migrations (`Gateway.Infrastructure/Data/Migrations/`)."

- [x] **Passo 6: `docs/architecture/review-checklist.md`**

- BE-08: o link `backend.md#migrations-a-partir-da-fase-01` vira `backend.md#migrations`.
- Acrescentar ao fim da tabela Backend:

```markdown
| BE-19 | 🟡 | 01 | Mapeamento fora de `Mappings/` | `.Adapt(`, `TypeAdapterConfig` ou mapeamento manual campo a campo de entidade → DTO fora de `Application/Mappings/` e `DependencyInjection.cs` | [Mapeamento](backend.md#mapeamento-mapster) |
| BE-20 | 🔴 | 01 | Options sem validação | Classe `*Options` registrada sem `ValidateOnStart()` ou chave obrigatória sem `[Required]` | [Injeção de dependência por camada](backend.md#injeção-de-dependência-por-camada) |
| BE-21 | 🟡 | 01 | Versão de pacote no `.csproj` | `PackageReference` com atributo `Version` (versões só em `Directory.Packages.props`) | [Dependências entre camadas](backend.md#dependências-entre-camadas) |
```

- [x] **Passo 7: ADR 0005 e índice**

`docs/architecture/decisions/0005-mapster.md`:

```markdown
# 0005 — Mapster no lugar do AutoMapper

**Data:** 2026-10-16
**Status:** Aceito
**Fase:** 01-higiene-fundacao

## Contexto

O Gateway usava AutoMapper 14 para dois mapeamentos (`City → CityDto`, `User → UserDto`). A partir da v15 o AutoMapper passou a ter licença comercial; a v14, gratuita, ficou congelada, sem atualização para o .NET 10 e com alerta de vulnerabilidade publicado em 2025.

## Decisão

Usar **Mapster** (licença MIT), configurado por classes `IRegister` em `Application/Mappings/` e injetado como `MapsterMapper.IMapper`, em modo runtime.

## Alternativas consideradas

- **Mapeamento manual (métodos de extensão)** — zero dependências e explícito; preterido para manter um mapeador declarativo que escale se os DTOs crescerem.
- **Manter AutoMapper 14** — sem trabalho agora, mas congelado, com alerta de segurança e risco de licença.
- **Mapster com geração de código (`Mapster.Tool`)** — sem reflexão em runtime, mas exige passo extra de build; não compensa para dois mapeamentos. Evolução possível se o número de DTOs crescer.

## Consequências

- Dependências: `Mapster` e `Mapster.DependencyInjection` só na `Gateway.Application`.
- Mapeamentos novos entram em `Application/Mappings/` (regra BE-19 do checklist).
- A troca para geração de código no futuro não muda os chamadores (`IMapper.Map<T>`).
```

Em `docs/architecture/decisions/README.md`, acrescentar ao índice:

```markdown
| [0005](0005-mapster.md) | Mapster no lugar do AutoMapper | Aceito |
```

- [x] **Passo 8: Auditoria**

Em `docs/features/00-roteiro-portfolio/auditoria-inicial.md`:
- Na tabela "Código e estrutura", acrescentar após A22:

```markdown
| A28 | 🟡 | Camadas invertidas: `Gateway.Application` referencia `Gateway.Infrastructure`; EF Core/Npgsql/AutoMapper no Domain; cliente HTTP do Provider na Application (achado na Fase 01) | `*.csproj` | 01 | |
```

- Coluna **Status** = `✅ Fase 01` em A5, A8, A9, A10, A11, A12, A13, A20, A21, A22, A28; em A23 = `✅ Fase 01 (sln, appsettings, launchSettings) · Docker na 05`; em A27 = `🟡 URL na Fase 01 · resto na 07`.
- Os links da coluna "Onde" dos itens resolvidos continuam apontando para os caminhos antigos — trocar por texto simples sem link (ex.: `` `UserAppService.cs` ``) para não ficarem quebrados.
- Atualizar "`A1`…`A27`" para "`A1`…`A28`" na tabela de documentos do `CLAUDE.md`. O `design.md` da Fase 00 **não** muda (documento de fase concluída é congelado).

- [x] **Passo 9: `.claude/skills/revisar-padrao/SKILL.md`**

No item 3 de "Regras", trocar `` `WeatherGatewayApp/**` ou `WeatherProviderApp/**` `` por `` `src/**` ``; e no exemplo do relatório, `` `WeatherGatewayApp/.../CityAppService.cs:20` `` por `` `src/Gateway/.../CityAppService.cs:20` ``.

- [x] **Passo 10: `docs/project-overview.md` e `README.md`**

`project-overview.md`:
- Bloco de estrutura:

```
src/Gateway/   ← BFF: usuários, cidades, favoritos (EF Core + PostgreSQL)
src/Provider/  ← integração OpenWeather + Pexels (sem estado)
web/           ← React 19 + Vite + Ant Design
docs/          ← ver docs/README.md
```

- "Estado Atual": título `(2026-10-16, após a Fase 01)`; remover "Um clone do repositório não compila (A23)."; **Stack:** `.NET 10 · ASP.NET Core Web API · EF Core 10 + Npgsql · Mapster · OpenAPI + Scalar · Newtonsoft.Json (até a Fase 03) · React 19 · Vite 6 · Ant Design 5 · axios`.
- Título da fase: `#### Fase 01 — Higiene e fundação ✅ Concluída (AAAA-MM-DD)` (data do commit) e, abaixo, `Design: [design.md](features/01-higiene-fundacao/design.md) · Plano: [plan.md](features/01-higiene-fundacao/plan.md)` e a lista de auditoria `A5, A8–A13, A20–A23, A27 (URL), A28`.
- Fase 02: remover "A27 (URL da API)" da linha de auditoria.

`README.md` (raiz): na seção **Stack**, `.NET 10 · ASP.NET Core · EF Core · PostgreSQL · React · Vite · Ant Design`.

- [x] **Passo 11: Links e commit**

```bash
for f in $(git ls-files '*.md') ; do d=$(dirname "$f"); grep -oE '\]\([^)#[:space:]]+' "$f" | sed 's/](//' | grep -v '^http' | while read p; do [ -e "$d/$p" ] || echo "QUEBRADO $f -> $p"; done; done
```

Esperado: só o exemplo ilustrativo `caminho/relativo.cs:42` do `CLAUDE.md`. `/revisar-padrao`, então:

```bash
git add -A
git commit -m "Atualiza documentação da Fase 01" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(A data `✅ Concluída` usa a data deste commit.)

---

### Tarefa 7: Verificação final (sem commit)

- [x] **Passo 1: Build e front**

```bash
cd "/c/Program Files/source/WeaterApp"
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

Esperado: tudo sem erro.

- [x] **Passo 2: Clone limpo (A23)**

```bash
TMP=$(mktemp -d)
git clone -q --branch dev/higiene-fundacao "/c/Program Files/source/WeaterApp" "$TMP/climato"
dotnet build "$TMP/climato/Climato.slnx"
rm -rf "$TMP"
```

Esperado: `Build succeeded`.

- [x] **Passo 3: Startup sem segredo**

```bash
dotnet run --project src/Provider/Climato.Provider.Api --launch-profile https -- --OpenWeather:ApiKey=
```

Esperado: falha ao subir com `OpenWeather:ApiKey não configurada. Ver docs/runbook.md.` (o argumento de linha de comando sobrescreve o user-secret com vazio).

- [x] **Passo 4: Roteiro manual com banco novo**

Se existir um banco `climato` de testes anteriores, apagá-lo (`DROP DATABASE climato;`). Subir Provider, Gateway e web (runbook, seção 4) e conferir:

1. Log do Gateway mostra `Applying migration '..._Inicial'`.
2. `https://localhost:7001/scalar` e `https://localhost:7002/scalar` abrem, com os summaries em português.
3. No web (`http://localhost:5173`): cadastro → login → buscar "Cuiabá" (clima + imagem de fundo) → voltar: cidade no histórico → favoritar → aparece em Favoritos → desfavoritar → excluir (DevTools → Network: `DELETE /cities/{id}` = `204`).
4. No Scalar do Gateway: `DELETE /cities/{guid aleatório}` → `404`.
5. Excluir um usuário com cidades (`DELETE /users/{id}` no Scalar) → `204`; `GET /cities?userId={id}` → lista vazia (cascade).

- [x] **Passo 5: Relatório**

Registrar no fim deste plano uma seção `## Notas de execução (AAAA-MM-DD)` com desvios e achados, e seguir para `/requesting-code-review` → `/finishing-a-development-branch`.

## Notas de execução (2026-10-17)

Executado com um subagente por tarefa e revisão (spec + qualidade) após cada uma. Commits: `5f6ab81` (T1), `b13ef35` (T2), `15e906a` (T3), `171118d` (T4), `d281af7` (T5), `1b74bd6` + `6e5b113` (T6, com uma correção pós-revisão em `frontend.md`/`backend.md`).

**Verificação (Tarefa 7)**
- `dotnet build Climato.slnx`: 0 erros (avisos de nulabilidade pré-existentes em `Weather.cs`/`JsonDictionary.cs`); lint e build do web ok.
- Clone limpo da branch compila (A23).
- Provider sem chave não sobe: `OpenWeather:ApiKey não configurada. Ver docs/runbook.md.`
- Gateway criou o banco `climato` e aplicou `Inicial` no startup. O EF registra um `fail` de conexão antes do `CREATE DATABASE` quando o banco ainda não existe — ruído esperado.
- Fluxo pela API e pelo navegador: cadastro → login → clima de Cuiabá com imagem → histórico → favoritar/desfavoritar → `DELETE` `204` → id inexistente `404` → exclusão de usuário com cascade.
- A chave da OpenWeather não aparece nos logs: o .NET 10 mascara a query (`weather?*`).

**Desvios e achados**
- O certificado HTTPS de desenvolvimento não estava confiável na máquina; a chamada Gateway → Provider via `https://localhost:7002` falhou no TLS. A verificação foi feita com os serviços só em HTTP (`--urls http://localhost:500x`, `--Provider:BaseUrl=http://localhost:5002/`, `VITE_API_URL=http://localhost:5001`), sem mudança de código. O runbook já exige `dotnet dev-certs https --trust`.
- Visíveis e já mapeados para a Fase 03: vento em m/s exibido como km/h (A16), nascer/pôr do sol em UTC (A17).
- `dotnet new tool-manifest` criou o manifesto na raiz; movido para `.config/`.
- Pendências registradas no `project-overview.md`: lockfile do web (Fase 05), erro do `DELETE` não tratado no `CityCard.jsx` (Fase 07).
