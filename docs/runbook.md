# Runbook — Ambiente local

Estado **atual** (antes da Fase 01). Cada fase que mudar a forma de rodar atualiza esta página (regra GE-07). Alvo: `docker compose up` (Fase 05).

## Pré-requisitos

- .NET SDK 10 (compila os projetos `net8.0`; para **executar** é preciso o runtime ASP.NET Core 8 até a Fase 01 migrar para .NET 10)
- Node.js LTS (≥ 20)
- PostgreSQL rodando em `localhost:5432`
- Certificado HTTPS de desenvolvimento confiável: `dotnet dev-certs https --trust`
- Chaves gratuitas da [OpenWeather](https://openweathermap.org/api) e da [Pexels](https://www.pexels.com/api/)

## 1. Arquivos que não estão no repositório (A23)

O `.gitignore` atual ignora arquivos necessários. Até a Fase 01, criar localmente:

| Arquivo | Conteúdo mínimo |
|---|---|
| `WeatherProviderApp/WeatherProviderApp/appsettings.json` | `{ "OpenWeather": { "ApiKey": "<sua chave>" }, "Pexels": { "ApiKey": "<sua chave>" }, "AllowedHosts": "*" }` |
| `WeatherGatewayApp/WeatherGatewayApp/appsettings.json` | `{ "AllowedHosts": "*" }` |
| `*.sln` e `Properties/launchSettings.json` | Gerados pelo Visual Studio ao abrir os `.csproj` |

## 2. Banco de dados

1. Criar o banco `weather-app` no PostgreSQL.
2. Ajustar usuário/senha na connection string fixa em [`ConnectionContext.cs`](../WeatherGatewayApp/WeatherGatewayApp.Infrastructure/ConnectionContext.cs) (A5 — **não commitar a senha**).
3. Rodar [`Scripts/170520251436.sql`](../WeatherGatewayApp/Scripts/170520251436.sql) criando **primeiro a tabela `"user"`** e depois `city` (A9).

## 3. Subir os serviços

As portas são fixas no código: o web chama o Gateway em `https://localhost:44394` e o Gateway chama o Provider em `https://localhost:44313` — as portas SSL do perfil **IIS Express**.

| Serviço | Como subir | URL |
|---|---|---|
| Provider | Visual Studio, perfil IIS Express | `https://localhost:44313/swagger` |
| Gateway | Visual Studio, perfil IIS Express | `https://localhost:44394/swagger` |
| Web | `npm --prefix web install` e `npm --prefix web run dev` | `http://localhost:5173` |

## 4. Problemas comuns

| Sintoma | Causa |
|---|---|
| Toda busca mostra "cidade não encontrada" | Provider fora do ar ou chave da OpenWeather inválida — erros viram 404/500 indistintos (A14, A15) |
| Erro de FK ao rodar o script SQL | Ordem das tabelas no script (A9) |
| `ERR_CERT_AUTHORITY_INVALID` no navegador | Certificado de dev não confiável — `dotnet dev-certs https --trust` e abrir o Swagger do Gateway uma vez |

## 5. Verificação antes de PR

Comandos na seção [Verificação do CLAUDE.md](../CLAUDE.md#verificação).
