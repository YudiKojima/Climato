# Design — Fase 05: Containers e CI

**Data:** 2026-10-29
**Fase:** 05
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#05--containers-e-ci) · [ADR 0003](../../architecture/decisions/0003-deploy-gratuito.md) · [`backend.md`](../../architecture/backend.md) · [auditoria](../00-roteiro-portfolio/auditoria-inicial.md) (A23 Docker, A25 CI)
**Branch:** `dev/containers-ci`

---

## Objetivo

`docker compose up` sobe o Climato inteiro num clone limpo (gateway, provider, postgres, redis, web) e todo PR passa por CI (build e testes .NET, lint e build do web, build das imagens). As imagens são o artefato que a Fase 08 vai hospedar ([ADR 0003](../../architecture/decisions/0003-deploy-gratuito.md)).

**Fora do escopo:**
- Publicar imagens em registry → não há consumidor; Render/Fly (08) constroem do Dockerfile
- Deploy, domínio, HTTPS público → Fase 08
- Testes de integração no CI → `[Backlog]` ([plano](../04-testes/integracao-plan.md))
- Logs estruturados → Fase 06
- Healthcheck de container nas APIs → a imagem `aspnet` não tem `curl`; `/health/*` continuam disponíveis para a hospedagem

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Web → Gateway | **URL absoluta** no build (`VITE_API_URL=http://localhost:5001`), CORS liberando `http://localhost:8080` — mesma topologia da 08 (origens separadas) | Proxy `/api` no nginx (configuração diferente da Vercel) |
| Migrations | **Flag `Database:MigrateOnStartup`**; padrão `true` só em `Development`; o compose liga explicitamente | Container migrador com EF bundle; manter "≠ Production migra" |
| Imagens no CI | **Só build** em PR e push no `master` | Publicar no GHCR |
| Gerenciador do web | **npm** com `package-lock.json` versionado (o runbook já usa npm) | yarn (o `yarn.lock` local nunca foi versionado) |
| Ambiente no compose | APIs em **`Production`** (sem Scalar, sem user-secrets) | `Development` (exigiria user-secrets e liga Scalar) |
| Node | **24 LTS** (imagem e CI) | 22 |

---

## 1. Imagens

`Dockerfile` por serviço, multi-stage, e `.dockerignore` na raiz (contexto de build = raiz do repositório, por causa de `Directory.*.props`, `global.json` e projetos referenciados).

- **Gateway** (`src/Gateway/Climato.Gateway.Api/Dockerfile`) e **Provider** (`src/Provider/Climato.Provider.Api/Dockerfile`): estágio `mcr.microsoft.com/dotnet/sdk:10.0` copia os `.csproj` e props, faz `restore` (camada cacheável), copia o código e faz `publish -c Release`; estágio final `mcr.microsoft.com/dotnet/aspnet:10.0`, `USER $APP_UID` (não-root), porta **8080** (padrão das imagens .NET 8+), só HTTP dentro do container.
- **Web** (`web/Dockerfile`): estágio `node:24-alpine` com `npm ci` e `npm run build` recebendo `ARG VITE_API_URL`; estágio final `nginx:alpine` com `web/nginx.conf` (fallback de SPA: `try_files $uri /index.html`).
- `.dockerignore`: `**/bin`, `**/obj`, `**/node_modules`, `**/dist`, `.git`, `.vs`, `**/appsettings.Development.json`, `**/.env`, `docs`, `tests` (testes não entram nas imagens).

## 2. `docker-compose.yml` e `.env.example`

| Serviço | Imagem | Porta no host | Depende de |
|---|---|---|---|
| `postgres` | `postgres:17-alpine`, volume `climato-postgres` | — | healthcheck `pg_isready` |
| `redis` | `redis:7-alpine` | — | healthcheck `redis-cli ping` |
| `provider` | build | — (só o Gateway chama) | `redis` saudável |
| `gateway` | build | `5001:8080` | `postgres` saudável, `provider` iniciado |
| `web` | build (`VITE_API_URL=http://localhost:5001`) | `8080:80` | `gateway` iniciado |

Variáveis (padrão `Secao__Chave`):
- **provider:** `OpenWeather__ApiKey`, `Pexels__ApiKey` (do `.env`), `ConnectionStrings__Redis=redis:6379,abortConnect=false,connectTimeout=1000`, `OpenWeather__BaseUrl`/`Pexels__BaseUrl` herdados do `appsettings.json`.
- **gateway:** `ConnectionStrings__Climato` montada com `POSTGRES_PASSWORD`, `Jwt__SecretKey` (do `.env`), `Provider__BaseUrl=http://provider:8080/`, `Cors__AllowedOrigins__0=http://localhost:8080`, `Database__MigrateOnStartup=true`.
- **postgres:** `POSTGRES_DB=climato`, `POSTGRES_PASSWORD` (do `.env`).

`.env.example` na raiz com `OPENWEATHER_API_KEY`, `PEXELS_API_KEY`, `JWT_SECRET_KEY` (≥ 32 caracteres) e `POSTGRES_PASSWORD`, sem valores reais. O `.env` já é ignorado (`**/.env`). O compose falha com mensagem clara se faltar variável obrigatória (`${VAR:?mensagem}`).

## 3. Código

- **Migrations:** `Program.cs` do Gateway troca `if (!app.Environment.IsProduction())` por `if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))`. Desenvolvimento local continua migrando sem configuração; `Production` só migra com a flag. Resolve a pendência `[Fase 05]` de migration em produção. (Startup não tem teste unitário; a verificação é o `docker compose up` num banco vazio.)
- **Health checks:** timeout de **3 s** por check (Postgres, Provider, Redis), para um check travado não segurar o `/health/ready`. Resolve a pendência `[Fase 05]` de timeout.
- Nenhuma outra mudança em `src/`.

## 4. CI — `.github/workflows/ci.yml`

Em `pull_request` e `push` no `master`, três jobs em paralelo no `ubuntu-latest`:

| Job | Passos |
|---|---|
| `backend` | `actions/setup-dotnet` (`dotnet-version: 10.0.x`, compatível com o `global.json`), `dotnet build Climato.slnx`, `dotnet test --solution Climato.slnx` |
| `web` | `actions/setup-node` (24, cache npm), `npm ci`, `npm run lint`, `npm run build` (em `web/`) |
| `docker` | `docker compose build` |

`concurrency` cancela execuções antigas do mesmo PR. Exigir o CI verde para mergear (regra do `CLAUDE.md` a partir da 05) é uma **proteção de branch** que o dono do repositório ativa no GitHub; o runbook traz o passo a passo.

## 5. Documentação

- **`runbook.md`:** nova seção principal "Subir com Docker" (`cp .env.example .env`, preencher, `docker compose up --build`, URLs, `docker compose down -v` para zerar o banco); o caminho `dotnet run` continua como desenvolvimento; proteção de branch.
- **`CLAUDE.md` › Verificação:** acrescentar `docker compose build` e trocar a frase das fases.
- **`backend.md`:** `Database:MigrateOnStartup` em **Migrations** e **Configuração**; timeout dos health checks; nota de containers (porta 8080, `Production`, variáveis).
- **`system-design.md`:** diagrama/tabela de execução com o compose.
- **`frontend.md`:** `VITE_API_URL` fixado no build da imagem; npm + `package-lock.json`.
- **`auditoria-inicial.md`:** A23 ✅ (Docker), A25 parcial (CI e health checks ✅; logs na 06).
- **`project-overview.md`:** Fase 05 ✅; pendências `[Fase 05]` resolvidas removidas.

## Riscos

| Risco | Mitigação |
|---|---|
| `UseHttpsRedirection` sem porta HTTPS no container | Só registra aviso e não redireciona; mantido (o front usa HTTP no compose) |
| `VITE_API_URL` fixo na imagem | Build arg; a 08 constrói com a URL pública |
| Cota da OpenWeather/Pexels ao testar o compose | Cache do Provider; chaves do próprio avaliador |
| Primeira subida lenta (pull + build) | Documentado no runbook |
