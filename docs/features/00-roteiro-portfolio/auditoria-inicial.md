# Auditoria inicial — estado do projeto em 2026-10-16

Levantamento feito antes do roteiro de portfólio ([`design.md`](design.md)), sobre o commit `72e12a8`. Cada achado tem um código `A*` citado nos designs, planos e PRs das fases. A coluna **Fase** diz onde ele é resolvido; ao concluir a fase, marcar a coluna **Status** com ✅ e o número do PR.

**Severidade:** 🔴 crítico (segurança ou quebra) · 🟡 importante · 🔵 melhoria

## Segurança

| Código | Sev. | Achado | Onde | Fase | Status |
|---|---|---|---|---|---|
| A1 | 🔴 | Senha gravada em texto puro e comparada com `!=` | [UserAppService.cs:30](../../../WeatherGatewayApp/WeatherGatewayApp.Application/Services/UserAppService.cs) | 02 | |
| A2 | 🔴 | Login via `GET /users?email=&password=` — senha na URL, em logs e no histórico do navegador | [UserController.cs](../../../WeatherGatewayApp/WeatherGatewayApp/Controllers/UserController.cs), [api.js](../../../web/src/services/api.js) | 02 | |
| A3 | 🔴 | `UserDto` devolve o campo `Password` em cadastro e login | [UserDto.cs](../../../WeatherGatewayApp/WeatherGatewayApp.Application/Model/Users/UserDto.cs) | 02 | |
| A4 | 🔴 | Sem autenticação: `userId` vem na query; qualquer um lê as cidades de outro usuário. `PATCH /cities/{id}/favorite` e `DELETE /cities/{id}`/`/users/{id}` não verificam dono (IDOR) | [CityController.cs](../../../WeatherGatewayApp/WeatherGatewayApp/Controllers/CityController.cs) | 02 | |
| A5 | 🟡 | Connection string fixa no código; senha antiga do banco local já está no histórico do git | [ConnectionContext.cs:13](../../../WeatherGatewayApp/WeatherGatewayApp.Infrastructure/ConnectionContext.cs) | 01 | |
| A6 | 🟡 | CORS `AllowAnyOrigin` e `UseCors` registrado depois de `UseAuthorization` | [Program.cs](../../../WeatherGatewayApp/WeatherGatewayApp/Program.cs) | 02 | |
| A7 | 🟡 | "Sessão" do front é só `userId` no `localStorage`; nada é validado no servidor | [ProtectedRoute.jsx](../../../web/src/router/ProtectedRoute.jsx) | 02 | |

## Bugs

| Código | Sev. | Achado | Onde | Fase | Status |
|---|---|---|---|---|---|
| A8 | 🔴 | `async void DeleteAsync` em `CityAppService` e `UserAppService`: o controller responde 200 antes de excluir e exceções derrubam o processo | [CityAppService.cs:20](../../../WeatherGatewayApp/WeatherGatewayApp.Application/Services/CityAppService.cs) | 01 | |
| A9 | 🔴 | Script SQL cria `city` (com FK para `user`) antes de `user`; coluna `createdat` é `without time zone` mas o mapping usa `with time zone` | [170520251436.sql](../../../WeatherGatewayApp/Scripts/170520251436.sql) | 01 | |
| A10 | 🟡 | `DbContext` instanciado com `new` dentro dos repositórios (fora do DI, sem escopo por request) | [CityRepository.cs](../../../WeatherGatewayApp/WeatherGatewayApp.Infrastructure/Data/Repositories/CityRepository.cs) | 01 | |
| A11 | 🟡 | URL do Provider fixa (`https://localhost:44313`) | [WeatherAppService.cs:20](../../../WeatherGatewayApp/WeatherGatewayApp.Application/Services/WeatherAppService.cs) | 01 | |
| A12 | 🟡 | `WeatherProviderApp.Infrastructure` órfão: usa `ConnectionContext`/`ICityRepository` que não existem no Provider e não é referenciado | [CityRepository.cs](../../../WeatherProviderApp/WeatherProviderApp.Infrastructure/Data/Repositories/CityRepository.cs) | 01 | |
| A13 | 🔵 | Controllers do Gateway com namespace `WeatherProviderApp.Controllers` | [Controllers/](../../../WeatherGatewayApp/WeatherGatewayApp/Controllers/) | 01 | |
| A14 | 🟡 | Provider: qualquer `HttpRequestException` vira 404 — inclusive chave inválida ou Pexels fora; falha da imagem derruba o clima inteiro | [WeatherController.cs](../../../WeatherProviderApp/WeatherProviderApp/Controllers/WeatherController.cs) | 03 | |
| A15 | 🟡 | Gateway: erro do Provider vira 500 (`EnsureSuccessStatusCode`); front mostra "cidade não encontrada" para qualquer falha | [WeatherAppService.cs](../../../WeatherGatewayApp/WeatherGatewayApp.Application/Services/WeatherAppService.cs) | 03 | |
| A16 | 🟡 | Velocidade do vento chega em m/s (OpenWeather `units=metric`) e é exibida como km/h | [WeatherDetails.jsx](../../../web/src/pages/WeatherDetails.jsx) | 03 | |
| A17 | 🟡 | Nascer/pôr do sol: `DateTime` sem `Kind`/fuso no Provider e `DateTime.Parse` dependente de cultura no Gateway; o horário exibido não é o local da cidade | [WeatherAppService.cs (Provider)](../../../WeatherProviderApp/WeatherProviderApp.Application/Services/WeatherAppService.cs) | 03 | |
| A18 | 🔵 | Front bloqueia excluir cidade favorita; backend permite | [CityCard.jsx](../../../web/src/components/CityCard.jsx) | 02 | |

## Código e estrutura

| Código | Sev. | Achado | Onde | Fase | Status |
|---|---|---|---|---|---|
| A19 | 🟡 | Parsing de JSON externo com `Dictionary<string, object>` + casts (Newtonsoft); Gateway reparseia o JSON do Provider do mesmo jeito em vez de DTO tipado | `Core/Collections/JsonDictionary.cs` (ambos) | 03 | |
| A20 | 🔵 | Registro duplicado `AddTransient` + `AddHttpClient` para a mesma interface | `Program.cs` (ambos) | 01 | |
| A21 | 🔵 | Pacotes no projeto errado: EF Core/Npgsql na API do Gateway e no Provider; AutoMapper no Provider sem uso | `*.csproj` | 01 | |
| A22 | 🟡 | .NET 8 sai de suporte em 2026-11-10; máquina de desenvolvimento só tem SDK 10 | `*.csproj` | 01 | |

## Repositório e entrega

| Código | Sev. | Achado | Onde | Fase | Status |
|---|---|---|---|---|---|
| A23 | 🔴 | `.gitignore` ignora `*.sln`, `appsettings.json`, `launchSettings.json`, `Dockerfile` e `.dockerignore`: um clone não compila nem roda | [.gitignore](../../../.gitignore) | 01 (sln, appsettings, launchSettings) · 05 (Docker) | |
| A24 | 🟡 | Nenhum projeto de testes | — | 04 | |
| A25 | 🟡 | Sem CI, sem Docker, sem health checks, sem logs estruturados | — | 05, 06 | |
| A26 | 🟡 | README só com o título; `web/README.md` é o template do Vite | [README.md](../../../README.md) | 08 | |

## Frontend

| Código | Sev. | Achado | Onde | Fase | Status |
|---|---|---|---|---|---|
| A27 | 🟡 | URL da API fixa em `api.js`; layout com largura fixa (`width: 1000`), estilos inline, `alert()` para erros, logout duplicado em várias páginas | [api.js](../../../web/src/services/api.js), `pages/*` | 02 (URL) · 07 (resto) | |
