# Design — Fase 02: Segurança

**Data:** 2026-10-17
**Fase:** 02
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#02--segurança) · [auditoria](../00-roteiro-portfolio/auditoria-inicial.md) · [pendências `[Fase 02]`](../../project-overview.md#pendências-futuras)
**Branch:** `dev/seguranca`

---

## Objetivo

Trocar a "sessão" de mentira (`userId` no `localStorage` e na query) por **autenticação JWT** com senha em hash, garantir que cada usuário só acesse os próprios dados e padronizar validação e erros. Depois desta fase o app pode ser exposto publicamente sem vazar senhas nem dados de outros usuários.

**Auditoria resolvida:** A1, A2, A3, A4, A6, A7, A18.

**Pendências `[Fase 02]` resolvidas:** ordem do pipeline (`UseCors` antes de auth), e-mail único sem diferenciar maiúsculas, `encodeURIComponent` em `getWeatherByCity`, `User` com construtor próprio.

**Fora do escopo:**
- Testes automatizados → Fase 04 (verificação desta fase é manual, ver [Verificação](#verificação))
- Refresh token, recuperação de senha, confirmação de e-mail, rate limiting
- Checagens impossíveis, nulabilidade e `JsonDictionary` → Fase 03
- Reorganização de pastas do front, `AuthProvider`, tratamento geral de erro do `CityCard` → Fase 07

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Hash de senha | `PasswordHasher<User>` (`Microsoft.Extensions.Identity.Core`, PBKDF2), com rehash no login quando `SuccessRehashNeeded` | BCrypt.Net-Next (dependência extra, rehash manual) |
| Sessão | Só **access token** JWT (60 min, configurável) no `localStorage`; expirou → 401 → logout ([ADR 0006](../../architecture/decisions/0006-jwt-localstorage-sem-refresh.md)) | Access + refresh token rotativo |
| Conta | `DELETE /users/{id}` vira `DELETE /users/me` | Remover o endpoint |
| Validação | FluentValidation + `ValidationFilter` próprio (action filter genérico) | Pacote SharpGrip AutoValidation; validação manual por action |
| Recurso de outro usuário | Repositório filtra por `userId` → **404** (não revela existência) | Carregar e comparar → 403 |
| Excluir favorita (A18) | Regra no domínio → **409**; front mantém o bloqueio visual | Permitir nos dois lados |
| Usuários existentes | Migration **apaga** os usuários (e as cidades, em cascata): senhas em texto puro não são convertíveis; só há dados locais de dev | Converter no primeiro login |
| Testes | Nenhum nesta fase | Unitários mínimos; integração |

---

## 1. API

| Rota | Auth | Resposta |
|---|---|---|
| `POST /auth/register` `{ email, name, password }` | anônima | 201 `AuthDto` · 400 validação · 409 e-mail já cadastrado |
| `POST /auth/login` `{ email, password }` | anônima | 200 `AuthDto` · 400 validação · 401 "E-mail ou senha inválidos." (mesma mensagem nos dois casos) |
| `DELETE /users/me` | JWT | 204 |
| `GET /cities` · `GET /cities/favorites` | JWT | 200 — só as cidades do usuário do token |
| `PATCH /cities/{id}/favorite` | JWT | 200 `CityDto` · 404 se não existir **ou** não for do usuário |
| `DELETE /cities/{id}` | JWT | 204 · 404 (idem) · **409** se favorita |
| `GET /weathers/{cityName}` | JWT | 200 `WeatherDto` · 404 — a cidade é gravada no histórico do usuário do token |

`AuthDto`: `{ token, expiresAt, user: { id, email, name } }`.

**Removidos:** `GET /users?email=&password=` (A2), `POST /users`, `DELETE /users/{id}`, `UserGetDto`, `UserDto.Password` (A3), parâmetro `userId` em todas as rotas (A4).

Toda resposta de erro é `ProblemDetails` (`application/problem+json`), inclusive 401/404 sem corpo próprio.

## 2. Backend por camada

### Domain
- **`User`**: `Password` → `PasswordHash`. Construtor `User(string email, string name)` gera `Id` e `CreatedAt` e normaliza o e-mail (`Trim().ToLowerInvariant()`); método `SetPasswordHash(string hash)`. Construtor vazio fica só para o EF.
- **`City.EnsureCanBeDeleted()`**: lança `DomainException("Não é possível remover uma cidade favorita.")` se `IsFavorite`.
- **`DomainException`** (`Domain/Exceptions/`): violação de regra de negócio → 409.
- **`ICityRepository.GetAsync(Guid id, Guid userId, CancellationToken)`**: substitui o `GetAsync(id)`; filtra por dono.
- **`IUserRepository`**: busca por e-mail recebe o e-mail já normalizado (a normalização é responsabilidade de quem chama, via a mesma regra do `User`); ganha `UpdateAsync` para o rehash.

### Application
- **`Model/Auth/`**: `RegisterPostDto`, `LoginPostDto`, `AuthDto`. `UserDto` sem `Password`.
- **`Validators/`**: `RegisterPostDtoValidator` (e-mail obrigatório, válido, ≤ 255; nome obrigatório, ≤ 255; senha 8–128) e `LoginPostDtoValidator` (e-mail e senha obrigatórios). Mensagens em português. Registro com `AddValidatorsFromAssembly` no `AddGatewayApplication`.
- **`IAuthAppService`** / `AuthAppService`:
  - `RegisterAsync` → `null` se o e-mail já existe (controller devolve 409); senão cria `User`, aplica `IPasswordHasher<User>.HashPassword`, grava e devolve `AuthDto`.
  - `LoginAsync` → `null` se usuário não existe ou `VerifyHashedPassword` = `Failed` (controller devolve 401); `SuccessRehashNeeded` → novo hash + `UpdateAsync`.
- **`ITokenService`** (`Contracts/`): `AuthToken Create(User user)` (`Token`, `ExpiresAt`).
- **`IUserAppService.DeleteAsync(userId)`**: usado por `DELETE /users/me`.
- **`ICityAppService`**: `ToggleFavoriteAsync(id, userId)` e `DeleteAsync(id, userId)`; `DeleteAsync` chama `city.EnsureCanBeDeleted()` antes de remover.
- **`JwtOptions`** (`Options/`, seção `Jwt`): `Issuer`, `Audience`, `SecretKey` (`[Required]`, `[MinLength(32)]`), `ExpirationMinutes` (padrão 60); `ValidateOnStart()`.
- Pacotes novos: `Microsoft.Extensions.Identity.Core`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`.

### Infrastructure
- **`Auth/JwtTokenService`**: implementa `ITokenService` com `JsonWebTokenHandler` (`Microsoft.IdentityModel.JsonWebTokens`), HMAC-SHA256, claims `sub` (id), `email`, `name`, `jti`.
- **`UserConfiguration`**: coluna `passwordhash` (sem `HasMaxLength` de 255 → texto livre).
- **Migration `HashSenhaUsuario`**: `DELETE FROM "user";` (cidades caem em cascata) e renomeia `password` → `passwordhash`.
- **`CityRepository.GetAsync(id, userId)`** com `Where(c => c.Id == id && c.UserId == userId)`.

### Api
- **`DependencyInjection.cs`** com `AddGatewayApi(IConfiguration)`:
  - `AddControllers(o => o.Filters.Add<ValidationFilter>())`
  - `AddAuthentication().AddJwtBearer(...)`: valida issuer, audience, assinatura e tempo de vida (`ClockSkew` 30 s), `MapInboundClaims = false`; parâmetros lidos de `JwtOptions`
  - `AddAuthorization()`, `AddProblemDetails()`, `AddExceptionHandler<GlobalExceptionHandler>()`
  - CORS: política `Web` com `WithOrigins(Cors:AllowedOrigins)`, `AllowAnyHeader`, `AllowAnyMethod`
  - OpenAPI com esquema de segurança Bearer (document transformer), para autenticar no Scalar
- **`Filters/ValidationFilter`**: para cada argumento da action, resolve `IValidator<T>`; se houver erros, devolve `ValidationProblemDetails` (400).
- **`Errors/GlobalExceptionHandler`** (`IExceptionHandler`): `DomainException` → 409 com a mensagem; qualquer outra → 500 "Erro interno." sem detalhes, com log.
- **`Extensions/ClaimsPrincipalExtensions.GetUserId()`**: lê `sub` como `Guid`.
- **Controllers**: `[Authorize]` em `CityController`, `WeatherController`, `UserController`; `AuthController` com `[AllowAnonymous]`. Nenhum recebe `userId` por parâmetro.
- **`Program.cs`**:
  ```
  UseExceptionHandler → UseStatusCodePages → UseHttpsRedirection
  → UseCors("Web") → UseAuthentication → UseAuthorization → MapControllers
  ```
- Pacote novo: `Microsoft.AspNetCore.Authentication.JwtBearer`.

### Configuração
- `appsettings.json` (versionado): `Jwt:Issuer` = `climato-gateway`, `Jwt:Audience` = `climato-web`, `Jwt:ExpirationMinutes` = 60, `Cors:AllowedOrigins` = `["http://localhost:5173"]`.
- `Jwt:SecretKey` **só** em user-secrets / `appsettings.Development.json`; sem ela o Gateway não sobe (`ValidateOnStart`).

## 3. Frontend

- **`services/session.js`** (único ponto que toca o `localStorage` para auth): `saveSession(authDto)` grava `token`, `expiresAt`, `userName`; `getToken()`; `clearSession()`; `hasValidSession()` = token presente e `expiresAt` no futuro (sem decodificar o JWT — quem valida é o servidor). A chave `userId` deixa de existir.
- **`services/api.js`**:
  - Instância única `axios.create({ baseURL: import.meta.env.VITE_API_URL })`.
  - Interceptor de request: `Authorization: Bearer <token>` quando houver token.
  - Interceptor de response: 401 em rota fora de `/auth/` → `clearSession()` + `window.location.assign("/signin")`.
  - Funções: `login`, `register`, `getAllCities`, `getAllFavorites`, `getWeatherByCity` (com `encodeURIComponent`), `toggleFavorite`, `deleteCity`. Nenhuma envia `userId`.
  - `getApiErrorMessage(err)`: primeiro erro de validação, senão `detail`, senão `title`, senão mensagem padrão.
- **`SignIn` / `SignUp`**: chamam `login`/`register` → `saveSession` → `/p`; erro via `message.error(getApiErrorMessage(err))` (sai o `alert()`); cadastro valida senha ≥ 8 no front.
- **`ProtectedRoute`**: `hasValidSession()`; **`SignOut`**: `clearSession()`.
- **`CityCard`**: mantém o bloqueio visual de favorita; 409 da API vira `message.error`.

## 4. Documentação

| Arquivo | Mudança |
|---|---|
| `backend.md` | Stack (Auth, Validação), remover marcações "(a partir da Fase 02)", seção de autenticação (claims, `GetUserId`, `ValidationFilter`, `DomainException`) |
| `frontend.md` | Remover marcação "(a partir da Fase 02)", citar `session.js` |
| `review-checklist.md` | `[Authorize]` + `userId` das claims; repositório filtrado por dono; erros em ProblemDetails; validator para todo DTO de entrada; front sem `userId` e sem `axios` direto |
| `runbook.md` | `dotnet user-secrets set "Jwt:SecretKey" ...`; aviso de que a migration apaga os usuários locais |
| `system-design.md` | Fluxo de autenticação (front → `/auth/login` → JWT → Gateway) |
| ADR `0006-jwt-localstorage-sem-refresh.md` | JWT no `localStorage`, sem refresh token: simplicidade vs. exposição a XSS; quando revisitar |
| `auditoria-inicial.md` / `project-overview.md` | Marcar A1–A4, A6, A7, A18 e as pendências `[Fase 02]`; fase `✅ Concluída` no commit final |

## Verificação

Automática (seção [Verificação](../../../CLAUDE.md#verificação)):

```bash
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

Manual (Scalar + navegador), com dois usuários A e B:

1. Gateway sem `Jwt:SecretKey` não sobe e a mensagem cita a chave.
2. Cadastro de A → 201 com token; cadastro repetido (inclusive com maiúsculas no e-mail) → 409.
3. Senha < 8 → 400 `ValidationProblemDetails`.
4. Login com senha errada e com e-mail inexistente → mesmo 401.
5. Coluna `passwordhash` no banco não contém a senha em texto.
6. `GET /cities` sem token → 401 ProblemDetails.
7. B tenta `PATCH`/`DELETE` em cidade de A → 404.
8. `DELETE` de cidade favorita → 409 com mensagem em português.
9. Preflight `OPTIONS` de `http://localhost:5173` → 204 com cabeçalhos CORS; de outra origem → sem cabeçalhos.
10. Front: login, busca, favoritar, remover, logout; token apagado/expirado → volta para `/signin`.
11. `DELETE /users/me` → 204 e as cidades do usuário somem.

## Riscos

- **Usuários locais apagados** pela migration — documentado no runbook; aceitável (sem produção).
- **XSS expõe o token** no `localStorage` — aceito e registrado no ADR 0006; React escapa saída por padrão e não há `dangerouslySetInnerHTML`.
- **Sem testes automatizados** até a Fase 04 — a verificação manual acima vira base dos testes de integração da Fase 04.
