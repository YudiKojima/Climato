# Fase 02 — Segurança: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** trocar a "sessão" por `userId` por autenticação JWT com senha em hash, garantir acesso só aos próprios dados e padronizar validação e erros em ProblemDetails, conforme o [design aprovado](design.md).

**Arquitetura:** primeiro a infraestrutura transversal da API (ProblemDetails, handler global, FluentValidation, CORS por configuração); depois autenticação (entidade `User`, migration, emissão e validação do JWT, `/auth`, `/users/me`); depois dono do recurso em cidades e clima (A4, A18); por fim o front e a documentação. Cada tarefa compila sozinha e vira um commit.

**Stack:** .NET 10 · Microsoft.AspNetCore.Authentication.JwtBearer 10.0.13 · Microsoft.Extensions.Identity.Core 10.0.13 · Microsoft.IdentityModel.JsonWebTokens 8.19.2 · FluentValidation 12.2.0 · React 19 + axios.

## Restrições globais

- Ler [`backend.md`](../../architecture/backend.md) e [`frontend.md`](../../architecture/frontend.md) antes de editar. O design vale mais que este plano em caso de conflito.
- **Sem projeto de testes** (decisão do design; Fase 04). Cada tarefa termina com `dotnet build Climato.slnx` sem erros; tarefas do front com `npm --prefix web run lint` e `npm --prefix web run build`. Verificação funcional na Tarefa 6.
- **Commits:** um por tarefa (1–5), com as mensagens indicadas. A aprovação deste plano é a autorização explícita para esses commits (mesma exceção da Fase 01 à regra "nunca commitar durante a fase"). Antes de cada commit, `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/seguranca`, a partir do `main` **depois** do merge de `dev/ajustes-docs-fase-01`.
- **Nunca** gravar `Jwt:SecretKey` em arquivo versionado nem exibi-la no terminal/chat.
- Fora do escopo: testes; refresh token; checagens impossíveis (`?? throw` nos construtores existentes), nulabilidade e `JsonDictionary` (Fase 03); reorganização de pastas do front (Fase 07).
- Código C#: chaves em todo `if`/`else`/`foreach`; campos privados `camelCase` sem `_`; namespace file-scoped; mensagens em português; `CancellationToken` sem default nas interfaces.
- Versões só em `Directory.Packages.props`; `.csproj` sem `Version`.

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| `saveSession` grava `userName` | Grava só `token` e `expiresAt` | Nada no front exibe o nome (YAGNI); entra quando a Fase 07 tiver header |
| `Microsoft.IdentityModel.JsonWebTokens` sem versão definida | `8.19.2` | Mesma versão que o JwtBearer 10.0.13 puxa (via `Protocols.OpenIdConnect 8.19.2`); misturar versões do IdentityModel causa falhas de validação |
| `CityCard`: só 409 vira `message.error` | Qualquer erro do `DELETE` vira `message.error(getApiErrorMessage(err))`; `alert()` da favorita vira `message.warning` | Mesmo bloco `catch`; resolve de quebra a pendência `[Fase 07]` do `CityCard` e a regra FE-05 no arquivo tocado |
| `CORS` por configuração | Lido direto de `Cors:AllowedOrigins` (sem classe `*Options`) | Lista vazia é válida (nenhuma origem liberada); nada a validar no startup |

## Mapa de arquivos

Caminhos relativos a `src/Gateway/` quando começam com `Climato.Gateway.*`.

| Arquivo | Ação | Tarefa |
|---|---|---|
| `Directory.Packages.props` | Modificar | 1, 2 |
| `Climato.Gateway.Domain/Exceptions/DomainException.cs` | Criar | 1 |
| `Climato.Gateway.Application/Climato.Gateway.Application.csproj` | Modificar | 1, 2 |
| `Climato.Gateway.Application/DependencyInjection.cs` | Modificar | 1, 2 |
| `Climato.Gateway.Api/DependencyInjection.cs` | Criar | 1, 2 |
| `Climato.Gateway.Api/Filters/ValidationFilter.cs` | Criar | 1 |
| `Climato.Gateway.Api/Errors/GlobalExceptionHandler.cs` | Criar | 1 |
| `Climato.Gateway.Api/Program.cs` | Modificar | 1, 2 |
| `Climato.Gateway.Api/appsettings.json` | Modificar | 1, 2 |
| `Climato.Gateway.Domain/Users/User.cs`, `IUserRepository.cs` | Modificar | 2 |
| `Climato.Gateway.Infrastructure/Data/Configurations/UserConfiguration.cs` | Modificar | 2 |
| `Climato.Gateway.Infrastructure/Data/Repositories/UserRepository.cs` | Modificar | 2 |
| `Climato.Gateway.Infrastructure/Data/Migrations/*_HashSenhaUsuario.cs` | Criar (via `dotnet ef`) | 2 |
| `Climato.Gateway.Infrastructure/Auth/JwtTokenService.cs` | Criar | 2 |
| `Climato.Gateway.Infrastructure/DependencyInjection.cs`, `.csproj` | Modificar | 2 |
| `Climato.Gateway.Application/Options/JwtOptions.cs` | Criar | 2 |
| `Climato.Gateway.Application/Model/Auth/{RegisterPostDto,LoginPostDto,AuthDto,AuthToken}.cs` | Criar | 2 |
| `Climato.Gateway.Application/Model/Users/UserDto.cs` | Modificar | 2 |
| `Climato.Gateway.Application/Model/Users/{UserPostDto,UserGetDto}.cs` | Remover | 2 |
| `Climato.Gateway.Application/Validators/{RegisterPostDtoValidator,LoginPostDtoValidator}.cs` | Criar | 2 |
| `Climato.Gateway.Application/Contracts/{IAuthAppService,ITokenService}.cs` | Criar | 2 |
| `Climato.Gateway.Application/Contracts/IUserAppService.cs`, `Services/UserAppService.cs` | Modificar | 2 |
| `Climato.Gateway.Application/Services/AuthAppService.cs` | Criar | 2 |
| `Climato.Gateway.Api/Controllers/AuthController.cs` | Criar | 2 |
| `Climato.Gateway.Api/Controllers/UserController.cs` | Modificar | 2 |
| `Climato.Gateway.Api/Extensions/ClaimsPrincipalExtensions.cs` | Criar | 2 |
| `Climato.Gateway.Api/OpenApi/BearerSecuritySchemeTransformer.cs` | Criar | 2 |
| `Climato.Gateway.Api/Climato.Gateway.Api.csproj` | Modificar | 2 |
| `Climato.Gateway.Domain/Cities/{City,ICityRepository}.cs` | Modificar | 3 |
| `Climato.Gateway.Infrastructure/Data/Repositories/CityRepository.cs` | Modificar | 3 |
| `Climato.Gateway.Application/Contracts/ICityAppService.cs`, `Services/CityAppService.cs` | Modificar | 3 |
| `Climato.Gateway.Api/Controllers/{CityController,WeatherController}.cs` | Modificar | 3 |
| `web/src/services/{session,api}.js` | Criar / reescrever | 4 |
| `web/src/pages/{SignIn,SignUp,SignOut,Home,Favorites}.jsx`, `web/src/router/ProtectedRoute.jsx`, `web/src/components/CityCard.jsx` | Modificar | 4 |
| `docs/**` | Modificar / criar | 5 |

---

### Tarefa 0: Preparar a branch

- [x] **Passo 1:** confirmar que o PR de `dev/ajustes-docs-fase-01` foi mergeado no `main` (`git log --oneline -3 origin/main` mostra `Ajusta índice de fases e auditoria após a Fase 01`). Se não foi, parar e avisar o usuário.
- [x] **Passo 2:** `/using-git-worktrees` → branch `dev/seguranca` a partir do `origin/main`.
- [x] **Passo 3:** copiar `docs/features/02-seguranca/design.md` e `plan.md` para a branch e commitar:

```bash
git add docs/features/02-seguranca
git commit -m "Adiciona design e plano da Fase 02 (segurança)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [x] **Passo 4:** gerar a chave local do JWT (não aparece no terminal):

```bash
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 48)" --project src/Gateway/Climato.Gateway.Api
```

---

### Tarefa 1: ProblemDetails, handler global, FluentValidation e CORS por configuração (A6)

**Interfaces produzidas:**
- `Climato.Gateway.Domain.Exceptions.DomainException(string message)` → vira 409.
- `Climato.Gateway.Api.DependencyInjection.AddGatewayApi(this IServiceCollection, IConfiguration)`.
- Política CORS `"Web"`.
- Todo argumento de action com `IValidator<T>` registrado é validado antes da action (400 `ValidationProblemDetails`).

- [x] **Passo 1: Pacotes** — em `Directory.Packages.props`, dentro do `<ItemGroup>`, em ordem alfabética:

```xml
    <PackageVersion Include="FluentValidation" Version="12.2.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.2.0" />
```

Em `Climato.Gateway.Application.csproj`, no `ItemGroup` de pacotes:

```xml
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
```

- [x] **Passo 2: `Climato.Gateway.Domain/Exceptions/DomainException.cs`**

```csharp
namespace Climato.Gateway.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
```

- [x] **Passo 3: registrar validators** — em `Climato.Gateway.Application/DependencyInjection.cs`, adicionar `using FluentValidation;` e, após os `AddScoped` dos AppServices:

```csharp
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
```

- [x] **Passo 4: `Climato.Gateway.Api/Filters/ValidationFilter.cs`**

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Climato.Gateway.Api.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(
                context.HttpContext,
                context.ModelState,
                StatusCodes.Status400BadRequest,
                "Dados inválidos.");

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
```

- [x] **Passo 5: `Climato.Gateway.Api/Errors/GlobalExceptionHandler.cs`**

```csharp
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

> No .NET 10 o middleware não loga exceções tratadas por `IExceptionHandler`; por isso o `LogError` explícito no ramo 500.

- [x] **Passo 6: `Climato.Gateway.Api/DependencyInjection.cs`**

```csharp
using Climato.Gateway.Api.Errors;
using Climato.Gateway.Api.Filters;

namespace Climato.Gateway.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>());
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Web", policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        return services;
    }
}
```

- [x] **Passo 7: `Climato.Gateway.Api/Program.cs` (reescrever)**

```csharp
using Climato.Gateway.Api;
using Climato.Gateway.Application;
using Climato.Gateway.Infrastructure;
using Climato.Gateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGatewayApi(builder.Configuration)
    .AddGatewayApplication()
    .AddGatewayInfrastructure(builder.Configuration);

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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Web");
app.UseAuthorization();

app.MapControllers();

app.Run();
```

- [x] **Passo 8: `Climato.Gateway.Api/appsettings.json`** — adicionar após a seção `Provider`:

```json
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
```

(lembrar da vírgula depois do `}` de `Provider`).

- [x] **Passo 9: Build**

```bash
dotnet build Climato.slnx
```

Esperado: `Build succeeded`, 0 erros.

- [x] **Passo 10: Commit** (após `/revisar-padrao`)

```bash
git add Directory.Packages.props src/Gateway
git commit -m "Padroniza erros em ProblemDetails, valida entrada e restringe CORS no Gateway

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 2: Hash de senha, JWT, `/auth` e `/users/me` (A1, A2, A3)

**Interfaces consumidas:** `DomainException`, `AddGatewayApi`, `ValidationFilter` (Tarefa 1).

**Interfaces produzidas:**
- `User(string email, string name)`, `static string User.NormalizeEmail(string)`, `void User.SetPasswordHash(string)`, propriedade `PasswordHash`.
- `IUserRepository.UpdateAsync(User user, CancellationToken cancellationToken)`.
- `ITokenService.Create(User user) : AuthToken` · `record AuthToken(string Token, DateTimeOffset ExpiresAt)`.
- `IAuthAppService.RegisterAsync(RegisterPostDto, CancellationToken) : Task<AuthDto?>` e `LoginAsync(LoginPostDto, CancellationToken) : Task<AuthDto?>`.
- `IUserAppService.DeleteAsync(Guid id, CancellationToken) : Task<bool>` (único método).
- `ClaimsPrincipal.GetUserId() : Guid` (lê `sub`).
- Rotas `POST /auth/register`, `POST /auth/login`, `DELETE /users/me`.

- [x] **Passo 1: Pacotes** — em `Directory.Packages.props`, em ordem alfabética:

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Identity.Core" Version="10.0.13" />
    <PackageVersion Include="Microsoft.IdentityModel.JsonWebTokens" Version="8.19.2" />
```

- `Climato.Gateway.Api.csproj`: `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />`
- `Climato.Gateway.Application.csproj`: `<PackageReference Include="Microsoft.Extensions.Identity.Core" />`
- `Climato.Gateway.Infrastructure.csproj`: `<PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" />`

- [x] **Passo 2: `Climato.Gateway.Domain/Users/User.cs` (reescrever)**

```csharp
namespace Climato.Gateway.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(string email, string name)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        Name = name.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }
}
```

- [x] **Passo 3: `IUserRepository` e `UserRepository`** — adicionar à interface:

```csharp
    Task UpdateAsync(User user, CancellationToken cancellationToken);
```

e à implementação (a entidade já está rastreada pelo contexto):

```csharp
    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
```

- [x] **Passo 4: `UserConfiguration.cs`** — trocar a linha de `Password` por:

```csharp
        builder.Property(e => e.PasswordHash).HasColumnName("passwordhash").IsRequired();
```

- [x] **Passo 5: `Climato.Gateway.Application/Options/JwtOptions.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Issuer não configurado. Ver docs/runbook.md.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt:Audience não configurado. Ver docs/runbook.md.")]
    public string Audience { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt:SecretKey não configurada. Ver docs/runbook.md.")]
    [MinLength(32, ErrorMessage = "Jwt:SecretKey deve ter pelo menos 32 caracteres.")]
    public string SecretKey { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpirationMinutes deve estar entre 1 e 1440.")]
    public int ExpirationMinutes { get; init; } = 60;
}
```

- [x] **Passo 6: DTOs** — remover `Model/Users/UserPostDto.cs` e `Model/Users/UserGetDto.cs`; em `UserDto.cs` remover a propriedade `Password`. Criar em `Climato.Gateway.Application/Model/Auth/`:

`RegisterPostDto.cs`
```csharp
namespace Climato.Gateway.Application.Model.Auth;

public class RegisterPostDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

`LoginPostDto.cs`
```csharp
namespace Climato.Gateway.Application.Model.Auth;

public class LoginPostDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

`AuthDto.cs`
```csharp
using Climato.Gateway.Application.Model.Users;

namespace Climato.Gateway.Application.Model.Auth;

public class AuthDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}
```

`AuthToken.cs`
```csharp
namespace Climato.Gateway.Application.Model.Auth;

public sealed record AuthToken(string Token, DateTimeOffset ExpiresAt);
```

- [x] **Passo 7: Validators** — `Climato.Gateway.Application/Validators/RegisterPostDtoValidator.cs`

```csharp
using Climato.Gateway.Application.Model.Auth;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class RegisterPostDtoValidator : AbstractValidator<RegisterPostDto>
{
    public RegisterPostDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(255).WithMessage("O e-mail deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o nome.")
            .MaximumLength(255).WithMessage("O nome deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe a senha.")
            .Length(8, 128).WithMessage("A senha deve ter entre 8 e 128 caracteres.");
    }
}
```

`LoginPostDtoValidator.cs`
```csharp
using Climato.Gateway.Application.Model.Auth;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class LoginPostDtoValidator : AbstractValidator<LoginPostDto>
{
    public LoginPostDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Informe a senha.");
    }
}
```

- [x] **Passo 8: Contratos** — `Contracts/ITokenService.cs`

```csharp
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.Application.Contracts;

public interface ITokenService
{
    AuthToken Create(User user);
}
```

`Contracts/IAuthAppService.cs`
```csharp
using Climato.Gateway.Application.Model.Auth;

namespace Climato.Gateway.Application.Contracts;

public interface IAuthAppService
{
    Task<AuthDto?> RegisterAsync(RegisterPostDto dto, CancellationToken cancellationToken);
    Task<AuthDto?> LoginAsync(LoginPostDto dto, CancellationToken cancellationToken);
}
```

`Contracts/IUserAppService.cs` (reescrever)
```csharp
namespace Climato.Gateway.Application.Contracts;

public interface IUserAppService
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

- [x] **Passo 9: `Services/AuthAppService.cs`**

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Model.Users;
using Climato.Gateway.Domain.Users;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;

namespace Climato.Gateway.Application.Services;

public class AuthAppService : IAuthAppService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher<User> passwordHasher;
    private readonly ITokenService tokenService;
    private readonly IMapper mapper;

    public AuthAppService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, ITokenService tokenService, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
        this.mapper = mapper;
    }

    public async Task<AuthDto?> RegisterAsync(RegisterPostDto dto, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(dto.Email);
        var allowCreate = await userRepository.AllowCreateAsync(email, cancellationToken);

        if (!allowCreate)
        {
            return null;
        }

        var user = new User(email, dto.Name);
        user.SetPasswordHash(passwordHasher.HashPassword(user, dto.Password));

        await userRepository.CreateAsync(user, cancellationToken);
        return CreateAuth(user);
    }

    public async Task<AuthDto?> LoginAsync(LoginPostDto dto, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(User.NormalizeEmail(dto.Email), cancellationToken);

        if (user == null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, dto.Password));
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        return CreateAuth(user);
    }

    private AuthDto CreateAuth(User user)
    {
        var token = tokenService.Create(user);

        return new AuthDto
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = mapper.Map<UserDto>(user)
        };
    }
}
```

- [x] **Passo 10: `Services/UserAppService.cs` (reescrever)**

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Domain.Users;

namespace Climato.Gateway.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository userRepository;

    public UserAppService(IUserRepository repository)
    {
        userRepository = repository;
    }

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
}
```

- [x] **Passo 11: DI da Application** — em `DependencyInjection.cs`, adicionar `using Climato.Gateway.Domain.Users;` e `using Microsoft.AspNetCore.Identity;`, e junto dos outros registros:

```csharp
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
```

- [x] **Passo 12: `Climato.Gateway.Infrastructure/Auth/JwtTokenService.cs`**

```csharp
using System.Text;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Options;
using Climato.Gateway.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Climato.Gateway.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions options;
    private readonly JsonWebTokenHandler tokenHandler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        this.options = options.Value;
    }

    public AuthToken Create(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpirationMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Name] = user.Name,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            }
        };

        return new AuthToken(tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
```

- [x] **Passo 13: DI da Infrastructure** — em `DependencyInjection.cs`, adicionar `using Climato.Gateway.Infrastructure.Auth;` e, após o registro de `ProviderOptions`:

```csharp
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();
```

- [x] **Passo 14: `Climato.Gateway.Api/Extensions/ClaimsPrincipalExtensions.cs`**

```csharp
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Climato.Gateway.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        return Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }
}
```

- [x] **Passo 15: `Climato.Gateway.Api/OpenApi/BearerSecuritySchemeTransformer.cs`**

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Climato.Gateway.Api.OpenApi;

public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token obtido em POST /auth/login ou POST /auth/register."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}
```

> Usa a API do `Microsoft.OpenApi` 2.x (que vem com o `Microsoft.AspNetCore.OpenApi` 10). Se o compilador acusar diferença de tipo em `SecuritySchemes`/`Security`, ajustar ao tipo exato indicado no erro, mantendo o comportamento: um esquema `Bearer` HTTP e um requisito global.

- [x] **Passo 16: `Climato.Gateway.Api/DependencyInjection.cs` (reescrever com autenticação)**

```csharp
using System.Text;
using Climato.Gateway.Api.Errors;
using Climato.Gateway.Api.Filters;
using Climato.Gateway.Api.OpenApi;
using Climato.Gateway.Application.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Climato.Gateway.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>());
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Web", policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
            });

        services.AddAuthorization();

        return services;
    }
}
```

- [x] **Passo 17: `Program.cs`** — inserir `app.UseAuthentication();` entre `app.UseCors("Web");` e `app.UseAuthorization();`.

- [x] **Passo 18: `appsettings.json`** — adicionar após `Cors` (sem `SecretKey`):

```json
  "Jwt": {
    "Issuer": "climato-gateway",
    "Audience": "climato-web",
    "ExpirationMinutes": 60
  }
```

- [x] **Passo 19: `Climato.Gateway.Api/Controllers/AuthController.cs`**

```csharp
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService appService;

    public AuthController(IAuthAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Cria uma conta e devolve o token de acesso.</summary>
    /// <response code="201">Conta criada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="409">E-mail já cadastrado.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthDto>> Register(RegisterPostDto dto, CancellationToken cancellationToken)
    {
        var auth = await appService.RegisterAsync(dto, cancellationToken);

        if (auth == null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "E-mail já cadastrado.");
        }

        return StatusCode(StatusCodes.Status201Created, auth);
    }

    /// <summary>Autentica o usuário e devolve o token de acesso.</summary>
    /// <response code="200">Autenticado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">E-mail ou senha inválidos.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthDto>> Login(LoginPostDto dto, CancellationToken cancellationToken)
    {
        var auth = await appService.LoginAsync(dto, cancellationToken);

        if (auth == null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha inválidos.");
        }

        return Ok(auth);
    }
}
```

- [x] **Passo 20: `Climato.Gateway.Api/Controllers/UserController.cs` (reescrever)**

```csharp
using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserAppService appService;

    public UserController(IUserAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Remove a conta do usuário autenticado e o histórico de cidades dele.</summary>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Conta removida.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(User.GetUserId(), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
```

- [x] **Passo 21: Build**

```bash
dotnet build Climato.slnx
```

Esperado: 0 erros. Se o Passo 15 falhar por tipo do `Microsoft.OpenApi`, corrigir conforme a nota do passo.

- [x] **Passo 22: Migration**

```bash
dotnet ef migrations add HashSenhaUsuario --project src/Gateway/Climato.Gateway.Infrastructure --startup-project src/Gateway/Climato.Gateway.Api --output-dir Data/Migrations
```

Substituir os corpos de `Up` e `Down` do arquivo `*_HashSenhaUsuario.cs` gerado (o `Designer` e o snapshot ficam como o EF gerou):

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"user\";");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "user",
                newName: "passwordhash");

            migrationBuilder.AlterColumn<string>(
                name: "passwordhash",
                table: "user",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "passwordhash",
                table: "user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.RenameColumn(
                name: "passwordhash",
                table: "user",
                newName: "password");
        }
```

- [x] **Passo 23: Build e smoke test**

```bash
dotnet build Climato.slnx
dotnet run --project src/Gateway/Climato.Gateway.Api --launch-profile https
```

Esperado: log `Applying migration '..._HashSenhaUsuario'`. No Scalar (`https://localhost:7001/scalar`): `POST /auth/register` → 201 com `token`; `POST /auth/login` com senha errada → 401 ProblemDetails; `DELETE /users/me` sem token → 401 ProblemDetails. Parar o serviço.

- [x] **Passo 24: Commit** (após `/revisar-padrao`)

```bash
git add Directory.Packages.props src/Gateway
git commit -m "Adiciona hash de senha e autenticação JWT no Gateway

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 3: Dono do recurso em cidades e clima; favorita não pode ser removida (A4, A18)

**Interfaces consumidas:** `ClaimsPrincipal.GetUserId()` (Tarefa 2), `DomainException` (Tarefa 1).

**Interfaces produzidas:**
- `ICityRepository.GetAsync(Guid id, Guid userId, CancellationToken)` (substitui `GetAsync(Guid id, ...)`).
- `ICityAppService.ToggleFavoriteAsync(Guid id, Guid userId, CancellationToken)` e `DeleteAsync(Guid id, Guid userId, CancellationToken)`.
- `City.EnsureCanBeDeleted()`.

- [x] **Passo 1: `City.cs`** — adicionar `using Climato.Gateway.Domain.Exceptions;` no topo e o método após `ToggleFavorite()`:

```csharp
    public void EnsureCanBeDeleted()
    {
        if (IsFavorite)
        {
            throw new DomainException("Não é possível remover uma cidade favorita.");
        }
    }
```

- [x] **Passo 2: `ICityRepository.cs`** — trocar `Task<City?> GetAsync(Guid id, CancellationToken cancellationToken);` por:

```csharp
    Task<City?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken);
```

- [x] **Passo 3: `CityRepository.cs`** — trocar o método `GetAsync(Guid id, ...)` por:

```csharp
    public async Task<City?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Cities
            .Where(c => c.Id == id && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }
```

- [x] **Passo 4: `ICityAppService.cs` (reescrever)**

```csharp
using Climato.Gateway.Application.Model.Cities;

namespace Climato.Gateway.Application.Contracts;

public interface ICityAppService
{
    Task<List<CityDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<CityDto>> GetAllFavoritesAsync(Guid userId, CancellationToken cancellationToken);
    Task<CityDto?> ToggleFavoriteAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}
```

- [x] **Passo 5: `CityAppService.cs`** — substituir `DeleteAsync` e `ToggleFavoriteAsync`:

```csharp
    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, userId, cancellationToken);

        if (city == null)
        {
            return false;
        }

        city.EnsureCanBeDeleted();
        await cityRepository.DeleteAsync(city, cancellationToken);
        return true;
    }
```

```csharp
    public async Task<CityDto?> ToggleFavoriteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetAsync(id, userId, cancellationToken);

        if (city == null)
        {
            return null;
        }

        city.ToggleFavorite();
        await cityRepository.UpdateAsync(city, cancellationToken);

        return mapper.Map<CityDto>(city);
    }
```

- [x] **Passo 6: `CityController.cs` (reescrever)**

```csharp
using Climato.Gateway.Api.Extensions;
using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Model.Cities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.Api.Controllers;

[ApiController]
[Route("cities")]
[Authorize]
public class CityController : ControllerBase
{
    private readonly ICityAppService appService;

    public CityController(ICityAppService appService)
    {
        this.appService = appService;
    }

    /// <summary>Retorna as cidades pesquisadas pelo usuário autenticado.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CityDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await appService.GetAllAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna as cidades favoritas do usuário autenticado.</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<List<CityDto>>> GetAllFavorites(CancellationToken cancellationToken)
    {
        var result = await appService.GetAllFavoritesAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Favorita/Desfavorita a cidade informada.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="200">Cidade atualizada.</response>
    /// <response code="404">Cidade não encontrada para o usuário.</response>
    [HttpPatch("{id}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityDto>> ToggleFavorite(Guid id, CancellationToken cancellationToken)
    {
        var city = await appService.ToggleFavoriteAsync(id, User.GetUserId(), cancellationToken);

        if (city == null)
        {
            return NotFound();
        }

        return Ok(city);
    }

    /// <summary>Remove a cidade informada do histórico.</summary>
    /// <param name="id">Id da cidade</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    /// <response code="204">Cidade removida.</response>
    /// <response code="404">Cidade não encontrada para o usuário.</response>
    /// <response code="409">Cidade favorita não pode ser removida.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await appService.DeleteAsync(id, User.GetUserId(), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
```

- [x] **Passo 7: `WeatherController.cs`** — adicionar `using Climato.Gateway.Api.Extensions;` e `using Microsoft.AspNetCore.Authorization;`, `[Authorize]` na classe, e trocar a action por:

```csharp
    /// <summary>Retorna os dados climáticos da cidade informada e a registra no histórico do usuário autenticado.</summary>
    /// <param name="cityName">Nome da cidade, exemplo: Cuiabá</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição</param>
    [HttpGet("{cityName}")]
    public async Task<ActionResult<WeatherDto>> GetWeather(string cityName, CancellationToken cancellationToken)
    {
        var weather = await appService.GetByCityAsync(cityName, User.GetUserId(), cancellationToken);

        if (weather == null)
        {
            return NotFound();
        }

        return Ok(weather);
    }
```

(O `if (weather == null)` é pendência da Fase 03; não mexer.)

- [x] **Passo 8: Build**

```bash
dotnet build Climato.slnx
```

Esperado: 0 erros. `grep -rn "userId" src/Gateway/Climato.Gateway.Api/Controllers` não deve encontrar parâmetro de action.

- [x] **Passo 9: Commit** (após `/revisar-padrao`)

```bash
git add src/Gateway
git commit -m "Restringe cidades e clima ao usuário autenticado e bloqueia remoção de favorita

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 4: Front com token JWT (A7)

**Interfaces consumidas:** rotas da API das Tarefas 2–3; `AuthDto` = `{ token, expiresAt, user }`; erros em ProblemDetails (`title`, `detail`, `errors`).

**Interfaces produzidas:** `session.js` → `saveSession(auth)`, `getToken()`, `clearSession()`, `hasValidSession()`; `api.js` → `login`, `register`, `getAllCities`, `getAllFavorites`, `getWeatherByCity`, `toggleFavorite`, `deleteCity`, `getApiErrorMessage(error, fallback?)`.

- [x] **Passo 1: `web/src/services/session.js`**

```js
const TOKEN_KEY = "token";
const EXPIRES_AT_KEY = "expiresAt";

export const saveSession = (auth) => {
  localStorage.setItem(TOKEN_KEY, auth.token);
  localStorage.setItem(EXPIRES_AT_KEY, auth.expiresAt);
};

export const getToken = () => localStorage.getItem(TOKEN_KEY);

export const clearSession = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRES_AT_KEY);
};

export const hasValidSession = () => {
  const token = getToken();
  const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);

  return Boolean(token && expiresAt && new Date(expiresAt) > new Date());
};
```

- [x] **Passo 2: `web/src/services/api.js` (reescrever)**

```js
import axios from "axios";
import { clearSession, getToken } from "./session";

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });

api.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthRoute = error.config?.url?.startsWith("/auth/");

    if (error.response?.status === 401 && !isAuthRoute) {
      clearSession();
      window.location.assign("/signin");
    }

    return Promise.reject(error);
  }
);

export const login = async (data) => {
  const response = await api.post("/auth/login", data);
  return response.data;
};

export const register = async (data) => {
  const response = await api.post("/auth/register", data);
  return response.data;
};

export const getAllCities = async () => {
  const response = await api.get("/cities");
  return response.data;
};

export const getAllFavorites = async () => {
  const response = await api.get("/cities/favorites");
  return response.data;
};

export const getWeatherByCity = async (cityName) => {
  const response = await api.get(`/weathers/${encodeURIComponent(cityName)}`);
  return response.data;
};

export const toggleFavorite = async (id) => {
  const response = await api.patch(`/cities/${id}/favorite`);
  return response.data;
};

export const deleteCity = async (id) => {
  await api.delete(`/cities/${id}`);
};

export const getApiErrorMessage = (
  error,
  fallback = "Não foi possível concluir a operação."
) => {
  const data = error.response?.data;
  const firstValidationError = data?.errors
    ? Object.values(data.errors).flat()[0]
    : undefined;

  return firstValidationError || data?.detail || data?.title || fallback;
};
```

- [x] **Passo 3: `web/src/pages/SignIn.jsx`**
  - import: `import { login, getApiErrorMessage } from "../services/api";` e `import { hasValidSession, saveSession } from "../services/session";`
  - `handleSubmit`:

```jsx
  const handleSubmit = async (values) => {
    try {
      setIsLoading(true);
      const auth = await login(values);
      saveSession(auth);
      navigate("/p");
    } catch (err) {
      message.error(getApiErrorMessage(err, "E-mail ou senha incorretos."));
    } finally {
      setIsLoading(false);
    }
  };
```

  - `useEffect`:

```jsx
  useEffect(() => {
    if (hasValidSession()) navigate("/p");
  }, [navigate]);
```

- [x] **Passo 4: `web/src/pages/SignUp.jsx`**
  - import: `import { register, getApiErrorMessage } from "../services/api";` e `import { hasValidSession, saveSession } from "../services/session";`
  - `handleSubmit`:

```jsx
  const handleSubmit = async (values) => {
    try {
      setIsLoading(true);
      const auth = await register(values);
      saveSession(auth);
      navigate("/p");
    } catch (err) {
      message.error(getApiErrorMessage(err, "Erro ao criar conta."));
    } finally {
      setIsLoading(false);
    }
  };
```

  - `useEffect` igual ao do SignIn (Passo 3).
  - Regras do campo `password`:

```jsx
            rules={[
              { required: true, message: "Digite sua senha" },
              { min: 8, message: "A senha deve ter pelo menos 8 caracteres" },
            ]}
```

- [x] **Passo 5: `SignOut.jsx`, `Home.jsx`, `Favorites.jsx`** — importar `import { clearSession } from "../services/session";` e trocar `localStorage.removeItem("userId");` por `clearSession();`.

- [x] **Passo 6: `web/src/router/ProtectedRoute.jsx` (reescrever)**

```jsx
import { Navigate } from "react-router-dom";
import { hasValidSession } from "../services/session";

const ProtectedRoute = ({ children }) => {
  if (!hasValidSession()) {
    return <Navigate to="/signin" replace />;
  }

  return children;
};

export default ProtectedRoute;
```

- [x] **Passo 7: `web/src/components/CityCard.jsx`**
  - import: `import { toggleFavorite, deleteCity, getApiErrorMessage } from "../services/api";`
  - `handleDeleteClick`:

```jsx
  const handleDeleteClick = async (e) => {
    e.stopPropagation();

    if (city.isFavorite) {
      message.warning("Não é possível remover uma cidade favorita.");
      return;
    }

    try {
      await deleteCity(city.id);
    } catch (err) {
      message.error(getApiErrorMessage(err));
    } finally {
      onDelete();
    }
  };
```

- [x] **Passo 8: Conferências**

```bash
grep -rn "userId\|GetUser\|CreateUser\|DeleteCity" web/src
grep -rn "localStorage" web/src
npm --prefix web run lint
npm --prefix web run build
```

Esperado: primeiro grep vazio; `localStorage` só em `services/session.js`; lint e build sem erro.

- [x] **Passo 9: Commit** (após `/revisar-padrao`)

```bash
git add web/src
git commit -m "Usa token JWT no front e trata erros da API em ProblemDetails

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 5: Documentação da Fase 02

**Arquivos:** `docs/architecture/{backend,frontend,system-design,review-checklist}.md`, `docs/architecture/decisions/{README,0006-jwt-localstorage-sem-refresh}.md`, `docs/runbook.md`.

- [x] **Passo 1: `docs/architecture/backend.md`**
  - Tabela **Stack**: linha Validação → `| Validação | FluentValidation + \`ValidationFilter\` | — |`; linha Auth → `| Auth | JWT Bearer (\`Microsoft.AspNetCore.Authentication.JwtBearer\`) + \`PasswordHasher<User>\` | — |`.
  - Estrutura da solução: `Climato.Gateway.Api/` → `← Program.cs, DependencyInjection.cs, Controllers, Filters, Errors, Extensions, OpenApi`; Application acrescenta `Validators`; Infrastructure acrescenta `Auth (JwtTokenService)`; Domain acrescenta `Exceptions/`.
  - **Controllers**: remover "(a partir da Fase 02)"; trocar "(via FluentValidation, Fase 02)" por "(via `ValidationFilter`, que roda o `IValidator<T>` do argumento)"; acrescentar: "Usuário atual: `User.GetUserId()` (`Api/Extensions/ClaimsPrincipalExtensions`), que lê a claim `sub`."
  - **DTOs**: remover a exceção `UserDto.Password`; acrescentar: "Todo DTO de entrada tem um `AbstractValidator<T>` em `Application/Validators/`, com mensagens em português."
  - **Entidades**: trocar "— `User` ainda é montado campo a campo pelo `UserAppService` (ajuste na Fase 02)" por "; setters privados quando a entidade tem método de mudança (ex.: `User.SetPasswordHash`)". Acrescentar: "Violação de regra de negócio lança `DomainException` (vira 409)."
  - **Erros**: remover "(a partir da Fase 02)"; texto: "Respostas de erro em `ProblemDetails` (`AddProblemDetails` + `UseStatusCodePages` + `GlobalExceptionHandler`). `DomainException` → 409 com a mensagem; validação → 400 `ValidationProblemDetails`; resto → 500 \"Erro interno.\" sem detalhes. Erros esperados no controller com `Problem(statusCode, title)` ou `NotFound()`."
  - Nova seção em **Decisões Técnicas**:

```markdown
### Autenticação
JWT HS256 emitido pelo Gateway em `POST /auth/register` e `POST /auth/login` (claims `sub`, `email`, `name`, `jti`; validade `Jwt:ExpirationMinutes`). Sem refresh token ([ADR 0006](decisions/0006-jwt-localstorage-sem-refresh.md)). Senha com `PasswordHasher<User>` (PBKDF2), rehash no login quando o formato envelhece. E-mail normalizado (`User.NormalizeEmail`). Recurso de outro usuário responde 404, nunca 403. Pipeline: `UseExceptionHandler` → `UseStatusCodePages` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization`.
```

- [x] **Passo 2: `docs/architecture/frontend.md`**
  - Estrutura de pastas: `services/api.js  ← todas as chamadas HTTP` e nova linha `services/session.js ← token no localStorage (único ponto)`.
  - Chamadas à API: remover "(a partir da Fase 02)"; acrescentar: "Sessão só via `services/session.js` (`saveSession`, `getToken`, `clearSession`, `hasValidSession`); nenhum outro arquivo usa `localStorage` para auth. Mensagem de erro com `getApiErrorMessage(err)`."

- [x] **Passo 3: `docs/architecture/review-checklist.md`** — acrescentar:

```markdown
| BE-22 | 🟡 | 02 | DTO de entrada sem validator | `*PostDto`/`*PutDto` novo sem `AbstractValidator<T>` em `Application/Validators/` | [DTOs](backend.md#dtos-application) |
| BE-23 | 🟡 | 02 | Regra de negócio com status HTTP solto | AppService/controller devolvendo 409 por regra que pertence à entidade, em vez de `DomainException` | [Erros](backend.md#erros) |
```

na tabela Backend e, na tabela Frontend:

```markdown
| FE-11 | 🔴 | 02 | Sessão fora de `session.js` | `localStorage` com token/expiração fora de `services/session.js` | [Chamadas à API](frontend.md#chamadas-à-api) |
```

- [x] **Passo 4: `docs/architecture/system-design.md`**
  - Diagrama "Estado atual": `"HTTPS + userId na query"` → `"HTTPS + JWT (Bearer)"`.
  - Tabela **Dados**, linha `user`: `id, email (único, minúsculo), name, passwordhash (PBKDF2), createdat`.
  - Nova seção em **Fluxos**, antes de "Consulta de clima":

```markdown
### Autenticação

1. `POST /auth/register` ou `POST /auth/login` → Gateway valida (FluentValidation), confere o hash e devolve `{ token, expiresAt, user }`.
2. O web guarda token e expiração (`services/session.js`) e envia `Authorization: Bearer` em toda chamada.
3. O Gateway valida assinatura, emissor, audiência e validade; o usuário vem da claim `sub`.
4. Token ausente ou expirado → 401 → o web limpa a sessão e volta para `/signin`.
```

- [x] **Passo 5: ADR `docs/architecture/decisions/0006-jwt-localstorage-sem-refresh.md`**

```markdown
# 0006 — JWT no localStorage, sem refresh token

**Data:** 2026-10-18
**Status:** Aceito
**Fase:** 02-seguranca

## Contexto

A "sessão" era só o `userId` no `localStorage`, enviado na query e nunca validado (A4, A7). A Fase 02 precisa de autenticação real sem inflar o escopo de um projeto de portfólio com um único front SPA.

## Decisão

O Gateway emite um **access token JWT** (HS256, 60 min configuráveis) em `/auth/register` e `/auth/login`. O web guarda o token no `localStorage` e o envia como `Authorization: Bearer`. **Não há refresh token**: expirou, a API responde 401 e o usuário faz login de novo.

## Alternativas consideradas

- **Access + refresh token rotativo** — sessão longa sem novo login, com revogação; exige tabela, endpoint `/auth/refresh` e lógica de renovação no front. Quase dobra o escopo da fase.
- **Cookie `HttpOnly` + `SameSite`** — token fora do alcance de JavaScript (protege contra XSS), mas exige proteção CSRF e configuração de cookie entre origens (web e Gateway em portas/domínios diferentes no deploy gratuito).

## Consequências

- Um XSS consegue ler o token. Mitigação: React escapa a saída por padrão, o projeto não usa `dangerouslySetInnerHTML`, e o token expira em 60 min.
- Não há logout no servidor: um token vazado vale até expirar.
- Revisitar se o app ganhar sessões longas, múltiplos clientes ou dados sensíveis — caminho natural é refresh token em cookie `HttpOnly`.
```

  E no índice de `decisions/README.md`:

```markdown
| [0006](0006-jwt-localstorage-sem-refresh.md) | JWT no localStorage, sem refresh token | Aceito |
```

- [x] **Passo 6: `docs/runbook.md`**
  - Seção 2, no bloco de comandos, após a connection string:

```bash
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 48)" --project src/Gateway/Climato.Gateway.Api
```

  e o parágrafo: "A `Jwt:SecretKey` precisa de pelo menos 32 caracteres; o comando acima gera uma aleatória sem exibi-la. Trocar a chave invalida os tokens emitidos."
  - Seção 3, após o parágrafo da migração automática:

```markdown
> A migration `HashSenhaUsuario` (Fase 02) **apaga os usuários existentes** (e as cidades deles): as senhas antigas estavam em texto puro e não podem ser convertidas. Basta cadastrar-se de novo.
```

  - Tabela de problemas comuns: remover a linha "Busca retorna 500 depois de recriar o banco"; acrescentar:

```markdown
| `OptionsValidationException: Jwt:SecretKey não configurada` | Faltou o user-secret do JWT (seção 2) |
| Front volta para o login logo após entrar | Token expirado ou chave do JWT trocada — entrar de novo |
| Erro de CORS no console do navegador | Origem do web fora de `Cors:AllowedOrigins` no `appsettings.json` do Gateway |
```

- [x] **Passo 7: Commit** (após `/revisar-padrao`)

```bash
git add docs
git commit -m "Documenta autenticação, validação e erros da Fase 02

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarefa 6: Verificação final (sem commit)

- [ ] **Passo 1: Build e front**

```bash
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

Esperado: tudo sem erro.

- [ ] **Passo 2: Startup sem segredo**

```bash
dotnet run --project src/Gateway/Climato.Gateway.Api --launch-profile https -- --Jwt:SecretKey=
```

Esperado: não sobe; mensagem cita `Jwt:SecretKey`.

- [ ] **Passo 3: Roteiro manual** — subir Provider, Gateway e web (runbook, seção 4). Usuários A e B criados pelo Scalar ou pelo web.

1. Log mostra `Applying migration '..._HashSenhaUsuario'`.
2. `POST /auth/register` de A → 201 com `token`; repetir com o e-mail em maiúsculas → 409.
3. Cadastro com senha de 7 caracteres → 400 `ValidationProblemDetails` com mensagem em português.
4. Login com senha errada e com e-mail inexistente → mesmo 401 "E-mail ou senha inválidos.".
5. `SELECT email, passwordhash FROM "user";` → hash (começa com `AQAAAA`), nunca a senha.
6. `GET /cities` sem token → 401 em `application/problem+json`.
7. Com o token de B: `PATCH /cities/{id de A}/favorite` e `DELETE /cities/{id de A}` → 404.
8. `DELETE` de cidade favorita de A (com token de A) → 409 "Não é possível remover uma cidade favorita.".
9. CORS:
   ```bash
   curl -k -i -X OPTIONS https://localhost:7001/cities -H "Origin: http://localhost:5173" -H "Access-Control-Request-Method: GET"
   curl -k -i -X OPTIONS https://localhost:7001/cities -H "Origin: http://evil.example" -H "Access-Control-Request-Method: GET"
   ```
   Esperado: o primeiro com `Access-Control-Allow-Origin: http://localhost:5173`; o segundo sem esse cabeçalho.
10. Web: cadastro → busca "Cuiabá" → histórico → favoritar → Favoritos → tentar remover favorita (aviso) → desfavoritar → remover → sair. DevTools → Network: nenhuma URL com `userId`; requisições com `Authorization: Bearer`.
11. No DevTools, editar `expiresAt` no `localStorage` para uma data passada e recarregar → vai para `/signin`. Restaurar a data e trocar o `token` por um valor inválido → próxima chamada dá 401 e volta para `/signin`.
12. `DELETE /users/me` com o token de B → 204; `SELECT count(*) FROM city WHERE userid = '<id de B>';` → 0.

- [ ] **Passo 4: Relatório** — registrar no fim deste plano `## Notas de execução (AAAA-MM-DD)` com desvios e achados; seguir para `/requesting-code-review` → `/finishing-a-development-branch`. No commit final: Fase 02 `✅ Concluída` no `project-overview.md`, A1–A4, A6, A7, A18 marcados na auditoria, e removidas das "Pendências futuras" as quatro `[Fase 02]` e a `[Fase 07]` do `CityCard`.
