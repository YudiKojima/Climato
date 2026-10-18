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
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 48)" --project src/Gateway/Climato.Gateway.Api
dotnet user-secrets set "OpenWeather:ApiKey" "<sua chave>" --project src/Provider/Climato.Provider.Api
dotnet user-secrets set "Pexels:ApiKey" "<sua chave>" --project src/Provider/Climato.Provider.Api
```

A `Jwt:SecretKey` precisa de pelo menos 32 caracteres; o comando acima gera uma aleatória sem exibi-la. Trocar a chave invalida os tokens emitidos.

Se faltar alguma, a API não sobe e informa qual chave está ausente. `Provider:BaseUrl`, `OpenWeather:BaseUrl` e `Pexels:BaseUrl` precisam terminar com `/` — os clients usam caminhos relativos.

Frontend: copiar `web/.env.example` para `web/.env` — **obrigatório**: sem `VITE_API_URL` as chamadas vão para `undefined/...`.

## 3. Banco de dados

O banco `climato` é criado e migrado **automaticamente** quando o Gateway sobe fora de `Production`. Para aplicar manualmente:

```bash
dotnet ef database update --project src/Gateway/Climato.Gateway.Infrastructure --startup-project src/Gateway/Climato.Gateway.Api
```

> A migration `HashSenhaUsuario` (Fase 02) **apaga os usuários existentes** (e as cidades deles): as senhas antigas estavam em texto puro e não podem ser convertidas. Basta cadastrar-se de novo.

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
| `OptionsValidationException: Jwt:SecretKey não configurada` | Faltou o user-secret do JWT (seção 2) |
| Front volta para o login logo após entrar | Token expirado ou chave do JWT trocada — entrar de novo |
| Erro de CORS no console do navegador | Origem do web fora de `Cors:AllowedOrigins` no `appsettings.json` do Gateway |
| Toda busca mostra "cidade não encontrada" | Provider fora do ar ou chave inválida — erros ainda viram 404/500 indistintos (A14, A15, Fase 03) |
| `ERR_CERT_AUTHORITY_INVALID` no navegador | `dotnet dev-certs https --trust` e abrir `https://localhost:7001/scalar` uma vez |
| `fail: Microsoft.EntityFrameworkCore.Database.Connection` no primeiro startup | Esperado: o banco `climato` ainda não existe; o Gateway o cria e aplica as migrations logo em seguida |
| `The SSL connection could not be established` no log do Gateway | Certificado de desenvolvimento não confiável — `dotnet dev-certs https --trust` (seção Pré-requisitos) |

## 6. Verificação antes de PR

Comandos na seção [Verificação do CLAUDE.md](../CLAUDE.md#verificação).
