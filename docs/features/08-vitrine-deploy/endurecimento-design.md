# Design — Fase 08a: Endurecimento

**Data:** 2026-11-08
**Fase:** 08 (bloco **a** de 2; a **08b** é deploy, README, GIF e diagrama)
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#08--vitrine-e-deploy) · [ADR 0003](../../architecture/decisions/0003-deploy-gratuito.md) · [`backend.md`](../../architecture/backend.md) · [`frontend.md`](../../architecture/frontend.md) · pendências `[Fase 08]` do [`project-overview.md`](../../project-overview.md#pendências-futuras)
**Branch:** `dev/endurecimento-deploy`

---

## Objetivo

Deixar o Climato pronto para ficar exposto na internet: proteger as cotas da OpenWeather/Pexels, tirar o efeito colateral do `GET /weathers`, oferecer um usuário de demonstração e fechar as pendências `[Fase 08]` de borda pública (400 em inglês, `traceparent` do cliente, health checks, 503 lento com o Provider fora).

**Fora do escopo (08b):**
- Hospedagem, domínio, HTTPS, variáveis de produção.
- `ForwardedHeaders` (IP real atrás do proxy da hospedagem) — até lá o rate limit por IP usa `RemoteIpAddress`.
- README de portfólio, GIF e diagrama (A26).

**Fora do escopo (backlog):** homônimos, testes de integração, limpeza de duplicatas do histórico de desenvolvimento.

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Histórico | `GET /weathers/{nome}` **só lê**; registro via `POST /cities` explícito e idempotente, chamado pelo detalhe com o nome resolvido | `?record=false` nos cards (GET com efeito colateral continua); endpoint agregado (detalhe continua gravando via GET) |
| Custo dos cards | Cards pedem `GET /weathers/{nome}?image=false`: só OpenWeather, sem Pexels | Limitar cards com temperatura; endpoint agregado (mesmo nº de chamadas na OpenWeather); só rate limit |
| Rate limiting | `/auth/*`: 10 req/min por IP (janela fixa). Rotas autenticadas: 60 req/min por usuário (`sub`) | Limite global único (um visitante esgota a demo de todos) |
| Usuário demo | `Demo:Enabled` cria o usuário no startup com senha aleatória e cidades iniciais; `POST /auth/demo` devolve o token; botão no login | Credenciais públicas no README; sem usuário demo |
| `traceparent` | Gateway ignora o `traceparent` de entrada e sempre inicia trace novo; propagação Gateway → Provider igual | Sampler que ignora o pai remoto (o cliente ainda escolhe o TraceId) |

## Componentes

### 1. Histórico sem efeito colateral
- **Gateway:** `WeatherAppService.GetByCityAsync` deixa de receber `userId` e de gravar. `WeatherController` atualiza o `<summary>`.
- **Gateway:** `POST /cities` com `{ name, country }` (FluentValidation: `name` e `country` obrigatórios, ≤ 255, como as colunas de `city`). Cidade já no histórico do usuário → `200` com a existente; nova → `201` com `Location`. Usuário das claims.
- **Web:** `services/cities.js` ganha `addCity({ name, country })`; `WeatherDetails` chama ao carregar o clima (efeito sobre `weather.city`/`weather.country`). Falha no registro não derruba a tela: só `useApiFeedback` (erro inesperado).
- Sem migration. Concorrência (duas abas) continua no backlog do índice único.

### 2. Cards sem Pexels
- **Gateway:** `GET /weathers/{nome}?image=false` repassa `image=false` ao Provider (`IWeatherProviderClient.GetWeatherAsync(cityName, includeImage, ct)`).
- **Provider:** `WeatherAppService` pula o `ICityImageAppService` quando `includeImage` é `false`; `imageUrl` sai `null`. Chave de cache distinta (sufixo de imagem), para um card não servir o detalhe sem foto.
- **Web:** `getWeatherByCity(name, { image: false })`; `CityTemperature` usa essa forma (hook próprio ou parâmetro no `useWeather`).

### 3. Rate limiting (Gateway)
- `AddRateLimiter` com duas políticas nomeadas: `auth` (janela fixa, 10/min, partição por `RemoteIpAddress`) no `AuthController`; `api` (janela fixa, 60/min, partição pela claim `sub`) nos controllers autenticados. Health checks fora.
- Rejeição: `429` com `ProblemDetails` (`title`: "Muitas requisições. Tente novamente em instantes.") e `Retry-After`. Limites em `RateLimiting:*` no `appsettings.json` (Options validadas).
- Web: 429 é erro de negócio (status < 500) → `message.error` com o `title`, já coberto pelo `useApiFeedback`.

### 4. Usuário de demonstração
- **Gateway:** `Demo:Enabled` (padrão `false`; compose liga), `Demo:Email`, `Demo:Name`, `Demo:Cities` (lista inicial). Seeder no startup, depois das migrations: cria o usuário se não existir, com senha aleatória descartada (ninguém faz login por senha), e as cidades iniciais só quando o usuário é criado.
- `POST /auth/demo` (`[AllowAnonymous]`, política `auth`): demo ligada → mesmo contrato do login `{ token, expiresAt, user }`; desligada → `404`.
- **Web:** `VITE_DEMO_ENABLED=true` mostra o botão "Entrar como demonstração" no `SignIn`; usa o mesmo fluxo de sessão do login. Variável no `.env.example` e no build arg da imagem.
- Dados do demo são compartilhados entre visitantes — aceito; o README da 08b avisa.

### 5. 400 em português
- Erro de leitura do corpo (JSON malformado ou tipo errado) → `ValidationProblemDetails` com `errors` contendo só "JSON inválido." (sem mensagem do System.Text.Json nem nome de tipo .NET). Ajuste no `InvalidModelStateResponseFactory` existente; `AllowInputFormatterExceptionMessages = false` como segunda barreira.

### 6. `traceparent` do cliente
- Gateway registra um `DistributedContextPropagator` para o hosting que não extrai contexto de entrada (o hosting sempre cria trace raiz). A injeção no `HttpClient` (Gateway → Provider) continua pelo propagador padrão. Provider inalterado (não é borda pública).

### 7. Health checks
- Check do Provider no Gateway: client nomeado com timeout de 4 s e registro do check com timeout de 5 s (maior que os 3 s por check do Provider). Com Redis travado o Provider responde `Degraded` (200) dentro de ~3 s e o Gateway fica `Healthy`.

### 8. Provider fora do ar
- Hoje o 503 leva ~30 s. Causa investigada com `systematic-debugging` antes do fix (hipóteses: retentativas com espera sobre conexão recusada/DNS, `ConnectTimeout` padrão). Alvo: conexão recusada ou host inexistente → 503 em **≤ 5 s**, mantendo 25 s por tentativa para resposta lenta (orçamento do `backend.md`). Correção provável: `ConnectTimeout` curto no `SocketsHttpHandler` do client do Provider e/ou menos retentativas.

## Erros

| Situação | Resposta |
|---|---|
| Limite excedido | 429 + `Retry-After`, `ProblemDetails` em PT |
| `POST /auth/demo` com demo desligada | 404 |
| `POST /cities` inválido | 400 `ValidationProblemDetails` |
| JSON malformado | 400 com "JSON inválido." |
| Provider fora | 503 em ≤ 5 s |

## Testes (TDD)

- **Gateway (unit):** `WeatherAppService` não chama o repositório; `CityAppService.AddAsync` idempotente (cria / devolve existente); validador do `POST /cities`; `DemoAppService` (desligado → nulo; ligado → token do usuário demo); seeder (cria uma vez, cidades só na criação); fábrica do 400 sem mensagem em inglês; `image=false` repassado na URL do Provider.
- **Provider (unit):** `includeImage=false` não chama a Pexels e usa outra chave de cache.
- **Web (Vitest):** `addCity`, `getWeatherByCity` com `image=false`, sessão via `/auth/demo`.
- **Manual (compose):** 11ª chamada de login em 1 min → 429; Home abre sem criar histórico; detalhe cria; botão demo; `curl` com `traceparent` próprio devolve outro `X-Correlation-ID`; Provider parado → 503 rápido; Redis travado → Gateway `Healthy`.

## Documentação

- `backend.md`: rate limiting, `POST /cities`, `/auth/demo` e `Demo:*`, propagador do hosting, health/timeout do Provider, `image=false`.
- `frontend.md`: registro do histórico no detalhe, botão demo, `VITE_DEMO_ENABLED`.
- `review-checklist.md`: regra de GET sem efeito colateral e de rate limit em rota pública.
- `system-design.md`: fluxo de consulta de clima e de histórico atualizados.
- `runbook.md` / `.env.example`: `Demo__*`, `VITE_DEMO_ENABLED`.
- `project-overview.md`: pendências `[Fase 08]` resolvidas removidas; `ForwardedHeaders` registrado para a 08b.
