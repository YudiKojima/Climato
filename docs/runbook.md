# Runbook — Ambiente local

Alvo futuro: `docker compose up` (Fase 05). Até lá, os serviços sobem com `dotnet run`.

## Pré-requisitos

- .NET SDK 10 (versão fixada em `global.json`)
- Node.js LTS (≥ 20)
- PostgreSQL rodando em `localhost:5432`
- (Opcional) Redis em `localhost:6379` — sem ele o Provider usa cache em memória
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

## 3a. Redis (opcional)

Sem `ConnectionStrings:Redis`, o Provider usa cache em memória (por processo). Para usar Redis:

```bash
docker run -d --name climato-redis -p 6379:6379 redis:7-alpine
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379,abortConnect=false,connectTimeout=1000" --project src/Provider/Climato.Provider.Api
```

`abortConnect=false,connectTimeout=1000` evita que cada requisição espere 5 s quando o Redis cai: o Provider loga um warning e segue sem cache.

## 4. Subir a aplicação

Em três terminais:

```bash
dotnet run --project src/Provider/Climato.Provider.Api --launch-profile https
dotnet run --project src/Gateway/Climato.Gateway.Api --launch-profile https
npm --prefix web install && npm --prefix web run dev
```

| Serviço | URL | Documentação | Health |
|---|---|---|---|
| Provider | `https://localhost:7002` | `https://localhost:7002/scalar` | `/health/live` · `/health/ready` |
| Gateway | `https://localhost:7001` | `https://localhost:7001/scalar` | `/health/live` · `/health/ready` |
| Web | `http://localhost:5173` | — | — |

Visual Studio / Rider: abrir `Climato.slnx` e usar o perfil `https` de cada Api.

## 5. Testes

```bash
dotnet test --solution Climato.slnx
```

Roda os testes unitários de `tests/` (sem banco, Docker ou chaves). O `global.json` usa o runner Microsoft.Testing.Platform; por isso o comando é `dotnet test --solution`/`--project` (o modo VSTest não é suportado pelo xUnit v3 no SDK 10).

## 6. Problemas comuns

| Sintoma | Causa |
|---|---|
| `OptionsValidationException: OpenWeather:ApiKey não configurada` | Faltou o user-secret do Provider (seção 2) |
| `ConnectionStrings:Climato não configurada` | Faltou o user-secret do Gateway |
| `OptionsValidationException: Jwt:SecretKey não configurada` | Faltou o user-secret do JWT (seção 2) |
| Front volta para o login logo após entrar | Token expirado ou chave do JWT trocada — entrar de novo |
| Erro de CORS no console do navegador | Origem do web fora de `Cors:AllowedOrigins` no `appsettings.json` do Gateway |
| Busca mostra "Serviço de clima indisponível" | Provider fora do ar, chave da OpenWeather inválida ou cota estourada — ver o log do Provider e `https://localhost:7001/health/ready` |
| Warning `Falha ao ler o cache de clima` no log do Provider | Redis configurado mas fora do ar — subir o container ou remover `ConnectionStrings:Redis` |
| `ERR_CERT_AUTHORITY_INVALID` no navegador | `dotnet dev-certs https --trust` e abrir `https://localhost:7001/scalar` uma vez |
| `fail: Microsoft.EntityFrameworkCore.Database.Connection` no primeiro startup | Esperado: o banco `climato` ainda não existe; o Gateway o cria e aplica as migrations logo em seguida |
| `The SSL connection could not be established` no log do Gateway | Certificado de desenvolvimento não confiável — `dotnet dev-certs https --trust` (seção Pré-requisitos) |

## 7. Verificação antes de PR

Comandos na seção [Verificação do CLAUDE.md](../CLAUDE.md#verificação).
