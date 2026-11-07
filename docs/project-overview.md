# Climato — Project Overview

Rastreamento de fases e estado atual. **Não é a fonte de padrões de código** — para isso:

- [`architecture/system-design.md`](architecture/system-design.md) — serviços e comunicação
- [`architecture/backend.md`](architecture/backend.md) — padrões do backend
- [`architecture/frontend.md`](architecture/frontend.md) — padrões do frontend
- [`README.md`](README.md) — índice da documentação

---

## Visão Geral

O usuário cria uma conta, pesquisa uma cidade e vê o clima atual com uma foto da cidade ao fundo. Cada cidade pesquisada fica no seu histórico e pode ser favoritada ou removida.

Projeto criado na faculdade para praticar microsserviços e agora evoluído como **portfólio de backend .NET**. O roteiro completo e as decisões estão em [`features/00-roteiro-portfolio/design.md`](features/00-roteiro-portfolio/design.md).

```
src/Gateway/   ← BFF: usuários, cidades, favoritos (EF Core + PostgreSQL)
src/Provider/  ← integração OpenWeather + Pexels (sem estado)
web/           ← React 19 + Vite + Ant Design
docs/          ← ver docs/README.md
```

## Fluxo de Trabalho

Definido no [`CLAUDE.md`](../CLAUDE.md): brainstorming → `design.md` → `plan.md` → aprovação → branch `dev/...` → implementação → verificação → `/revisar-padrao` → PR. Ao concluir uma fase, marcar o título abaixo com `✅ Concluída (AAAA-MM-DD)` e os códigos resolvidos na [auditoria](features/00-roteiro-portfolio/auditoria-inicial.md).

---

## Estado Atual (2026-11-06, após a Fase 07)

**Funciona:** cadastro e login com senha em hash e JWT, cada usuário vê só os próprios dados, erros em ProblemDetails, busca de clima atual com imagem da cidade, histórico de cidades por usuário, favoritar/desfavoritar, remover cidade, erros distintos para cidade inexistente (404) e serviço indisponível (503), cache de clima e health checks, busca de cidades com lista de resultados, previsão de 5 dias com gráfico, temperatura nos cards e nomes de cidade em português, testes unitários nos dois serviços, `docker compose up` com os cinco serviços e CI em todo PR, logs JSON com correlation ID (TraceId) e traces OpenTelemetry no Jaeger (profile `observability`), web com tema próprio claro/escuro, layout responsivo, skeletons, estados vazio/erro com o código do erro e testes de lógica (Vitest).

**Não há:** testes de integração, métricas, deploy.

**Stack:** .NET 10 · ASP.NET Core Web API · EF Core 10 + Npgsql · Mapster · OpenAPI + Scalar · Microsoft.Extensions.Http.Resilience · IDistributedCache (Redis opcional) · React 19 · Vite 6 · Ant Design 5 · Recharts · axios · Vitest · Testing Library · xUnit v3 · Shouldly · NSubstitute · Docker · Docker Compose · nginx · GitHub Actions · Serilog · OpenTelemetry · Jaeger.

---

## Fases

#### Fase 00 — Roteiro do portfólio ✅ Concluída (2026-10-16)
Brainstorming do roteiro, auditoria inicial (A1–A27), estrutura de documentação, ADRs 0001–0004.
Design: [`features/00-roteiro-portfolio/design.md`](features/00-roteiro-portfolio/design.md)

#### Fase 01 — Higiene e fundação ✅ Concluída (2026-10-17)
.NET 10, solução única, `.gitignore`, DbContext via DI, configuração, EF Migrations, correção de bugs estruturais.
Design: [`design.md`](features/01-higiene-fundacao/design.md) · Plano: [`plan.md`](features/01-higiene-fundacao/plan.md)
Auditoria: A5, A8–A13, A20–A23, A27 (URL), A28.

#### Fase 02 — Segurança ✅ Concluída (2026-10-18)
Hash de senha, JWT, autorização por dono, CORS, validação, ProblemDetails; front adaptado ao token.
Design: [`design.md`](features/02-seguranca/design.md) · Plano: [`plan.md`](features/02-seguranca/plan.md)
Auditoria: A1–A4, A6, A7, A18.

#### Fase 03 — Comunicação entre serviços ✅ Concluída (2026-10-25)
Dividida em dois blocos:
- **03a — Contrato e robustez** ✅ (2026-10-19): DTOs tipados, erros 404/503, resiliência, cache (Redis opcional), health checks, vento em km/h e horários no fuso da cidade. [Design](features/03-comunicacao-servicos/design.md) · [Plano](features/03-comunicacao-servicos/plan.md)
- **03b — Novos endpoints** ✅ (2026-10-25): previsão de 5 dias e busca de cidades (geocoding), com `OpenWeatherClient` único e cache genérico no Provider. [Design](features/03-comunicacao-servicos/endpoints-design.md) · [Plano](features/03-comunicacao-servicos/endpoints-plan.md)

Auditoria: A14–A17, A19 (resolvidas na 03a).

#### Fase 04 — Testes ✅ Concluída (2026-10-29)
Reduzida a testes unitários para acelerar o roteiro: dois projetos (`Climato.{Gateway,Provider}.UnitTests`, 73 testes) cobrindo domínio, AppServices, cache, validators e mapeamento de erros. Integração (Testcontainers + WireMock) em backlog com plano pronto.
Design: [`design.md`](features/04-testes/design.md) · Plano: [`plan.md`](features/04-testes/plan.md) · Integração (backlog): [`integracao-plan.md`](features/04-testes/integracao-plan.md)
Auditoria: A24.

#### Fase 05 — Containers e CI ✅ Concluída (2026-10-31)
Dockerfiles multi-stage (Gateway, Provider, web com nginx), `docker-compose.yml` com Postgres e Redis, `.env.example`, flag `Database:MigrateOnStartup`, timeout nos health checks e GitHub Actions (backend, web, imagens).
Design: [`design.md`](features/05-containers-ci/design.md) · Plano: [`plan.md`](features/05-containers-ci/plan.md)
Auditoria: A23 (Docker), A25 (parcial: CI).

#### Fase 06 — Observabilidade ✅ Concluída (2026-11-04)
Serilog (JSON fora de Development), correlation ID = TraceId W3C (`X-Correlation-ID` e `traceId` no `ProblemDetails`), OpenTelemetry (ASP.NET Core, HttpClient, Npgsql) com Jaeger no profile `observability` do compose; log forging fechado pelo JSON.
Design: [`design.md`](features/06-observabilidade/design.md) · Plano: [`plan.md`](features/06-observabilidade/plan.md)
Auditoria: A25 (logs estruturados).

#### Fase 07 — Frontend moderno ✅ Concluída (2026-11-06)
Dividida em dois blocos:
- **07a — Fundação visual** ✅ (2026-11-05): tema Céu claro/escuro, AppLayout/AuthLayout, CSS Modules, skeletons, estados vazio/erro com correlation ID, Vitest. [Design](features/07-frontend-moderno/design.md) · [Plano](features/07-frontend-moderno/plan.md)
- **07b — Busca e previsão** ✅ (2026-11-06): busca de cidades com lista, previsão de 5 dias com gráfico (Recharts, carregado sob demanda), temperatura nos cards e nome da cidade em português (Provider resolve no geocoding e consulta por coordenadas). [Design](features/07-frontend-moderno/busca-previsao-design.md) · [Plano](features/07-frontend-moderno/busca-previsao-plan.md)

Auditoria: A27 (resolvida na 07a).

#### Fase 08 — Vitrine e deploy
Deploy gratuito, README de portfólio, GIF, diagrama.
Auditoria: A26.

---

## Pendências futuras

Itens descobertos durante as fases que não pertencem ao escopo delas. Formato: `- [Fase NN] descrição`.

- [Backlog] Mapster: trocar `TypeAdapterConfig.GlobalSettings` por instância própria e validar com `Compile()`/teste
- [Fase 08] Rate limiting em `/auth/*` (`AddRateLimiter`, janela fixa por IP) — obrigatório antes do deploy público
- [Backlog] Provider: resposta 200 incompleta da OpenWeather (lista `weather` vazia, `sys` nulo) vira 500 — lançar `ExternalServiceUnavailableException("resposta incompleta")` e cobrir com WireMock; `sys.country` ausente sai como `null`
- [Backlog] Gateway: 400 do Provider (`cityName` só com espaços em `/weathers` e `/forecasts`) vira 503 — tratar como 404 ou validar no Gateway
- [Backlog] Extrair a criação do `ValidationProblemDetails` duplicada entre `ValidationFilter` e `InvalidModelStateResponseFactory`
- [Backlog] Confirmar se o ramo 499 dos `GlobalExceptionHandler` é alcançado (o `ExceptionHandlerMiddleware` do .NET 8+ já trata `OperationCanceledException` com `RequestAborted`); remover ou ajustar o `backend.md`
- [Backlog] Índice único (`userid`, `name`) em `city`: duas buscas simultâneas da mesma cidade nova gravam duplicata
- [Fase 08] 400 automático expõe mensagens do System.Text.Json em inglês e nomes de tipo .NET em `errors` — desligar `AllowInputFormatterExceptionMessages` ou mapear para "JSON inválido."
- [Fase 08] Health checks: com Redis travado, o check do Gateway (3 s) pode estourar junto com o do Provider (3 s) e virar Unhealthy, contrariando "Redis fora não derruba o ready do Gateway" — dar ao Gateway um timeout maior que o do Provider; o `client.Timeout` de 5 s do health do Provider ficou inalcançável
- [Backlog] Previsão: resposta 200 da OpenWeather com `list` vazia ou `weather` vazio em algum ponto vira 500 — mesma correção da resposta incompleta do clima atual
- [Backlog] Testes de integração (WebApplicationFactory + Testcontainers Postgres/Redis + WireMock), plano em [`integracao-plan.md`](features/04-testes/integracao-plan.md); ao executar, incluir no CI da Fase 05 (Docker no runner)
- [Fase 08] `traceparent` vindo do cliente é aceito pelo Gateway (borda pública): o cliente escolhe o TraceId devolvido em `X-Correlation-ID` e pode desligar a amostragem da cadeia com a flag `-00` — avaliar descartar o `traceparent` de entrada no Gateway ou um sampler que ignore o pai remoto
- [Backlog] Bootstrap logger do Serilog (`CreateBootstrapLogger` + `try/catch`/`Log.CloseAndFlush` no `Program.cs`): falha no `MigrateAsync` ou antes do `Build()` sai como texto no stderr, fora do JSON; exige exceção da BE-30 para o `Program.cs`
- [Backlog] Teste de integração (junto do [`integracao-plan.md`](features/04-testes/integracao-plan.md)) garantindo que o `traceId` do JSON de erro é o TraceId puro e igual ao `X-Correlation-ID` (hoje só `ProblemDetailsTraceId.Apply` é testado isolado)
- [Backlog] Testes de componentes do web (render de telas com Testing Library)
- [Fase 08] Provider fora do ar: o Gateway leva ~30 s (retentativas + timeout total da resiliência) para devolver o 503, e a tela fica no skeleton esse tempo todo — reduzir o timeout total do client do Provider no Gateway
- [Backlog] Homônimos ("Springfield"): a busca mostra estado/país, mas clima e previsão usam a primeira cidade do geocoding — identificar por coordenadas exige mudar contrato, URL e entidade City
- [Fase 08] `GET /weathers/{nome}` tem efeito colateral (o Gateway grava a cidade no histórico quando `weather.City` não existe) e agora é chamado por cada card da Home: se o nome resolvido mudar (histórico gravado em inglês antes da 07b, `local_names.pt` ou primeiro resultado do geocoding mudando na OpenWeather), abrir a Home cria linhas novas sem ação do usuário — separar leitura de clima e registro no histórico (ex.: endpoint ou parâmetro sem persistência) e limpar duplicatas antes do deploy
- [Fase 08] Cards da Home fazem uma chamada `/weathers` por cidade (OpenWeather + Pexels com cache frio); com histórico grande e o cache de 10 min, isso pesa na cota gratuita — avaliar limite de cards com temperatura ou endpoint agregado no deploy público
