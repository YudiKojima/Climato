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

## Estado Atual (2026-10-19, após a Fase 03a)

**Funciona:** cadastro e login com senha em hash e JWT, cada usuário vê só os próprios dados, erros em ProblemDetails, busca de clima atual com imagem da cidade, histórico de cidades por usuário, favoritar/desfavoritar, remover cidade, erros distintos para cidade inexistente (404) e serviço indisponível (503), cache de clima e health checks.

**Não há:** testes, Docker, CI, logs estruturados, deploy.

**Stack:** .NET 10 · ASP.NET Core Web API · EF Core 10 + Npgsql · Mapster · OpenAPI + Scalar · Microsoft.Extensions.Http.Resilience · IDistributedCache (Redis opcional) · React 19 · Vite 6 · Ant Design 5 · axios.

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

#### Fase 03 — Comunicação entre serviços
Dividida em dois blocos ([design 03a](features/03-comunicacao-servicos/design.md)):
- **03a — Contrato e robustez** ✅ (2026-10-19): DTOs tipados, erros 404/503, resiliência, cache (Redis opcional), health checks, vento em km/h e horários no fuso da cidade.
- **03b — Novos endpoints**: previsão de 5 dias e busca de cidades (geocoding).

Auditoria: A14–A17, A19 (resolvidas na 03a).

#### Fase 04 — Testes
Unitários + integração (Testcontainers, WireMock).
Auditoria: A24.

#### Fase 05 — Containers e CI
Dockerfiles, docker-compose, GitHub Actions.
Auditoria: A23 (Docker), A25.

#### Fase 06 — Observabilidade
Serilog, correlation ID, OpenTelemetry opcional.
Auditoria: A25.

#### Fase 07 — Frontend moderno
Tema AntD próprio, modo escuro, responsivo, autocomplete, previsão com gráfico, skeletons.
Auditoria: A27.

#### Fase 08 — Vitrine e deploy
Deploy gratuito, README de portfólio, GIF, diagrama.
Auditoria: A26.

---

## Pendências futuras

Itens descobertos durante as fases que não pertencem ao escopo delas. Formato: `- [Fase NN] descrição`.

- [Fase 05] Versionar um lockfile do web e padronizar o gerenciador (hoje há um `yarn.lock` local não versionado e o runbook usa npm)
- [Fase 04] Mapster: trocar `TypeAdapterConfig.GlobalSettings` por instância própria e validar com `Compile()`/teste
- [Fase 05] Estratégia de migration em produção (bundle, job dedicado ou flag `Database:MigrateOnStartup`); hoje qualquer ambiente ≠ Production migra no startup
- [Fase 07] `CityCard.jsx`: `handleFavoriteClick` sem `try/catch` (falha no `PATCH` vira rejeição não tratada)
- [Fase 08] Rate limiting em `/auth/*` (`AddRateLimiter`, janela fixa por IP) — obrigatório antes do deploy público
- [Fase 04] Provider: resposta 200 incompleta da OpenWeather (lista `weather` vazia, `sys` nulo) vira 500 — lançar `ExternalServiceUnavailableException("resposta incompleta")` e cobrir com WireMock; `sys.country` ausente sai como `null`
- [Fase 04] Gateway: 400 do Provider (`cityName` só com espaços) vira 503 — tratar como 404 ou validar no Gateway
- [Fase 04] Extrair a criação do `ValidationProblemDetails` duplicada entre `ValidationFilter` e `InvalidModelStateResponseFactory`
- [Fase 04] Confirmar se o ramo 499 dos `GlobalExceptionHandler` é alcançado (o `ExceptionHandlerMiddleware` do .NET 8+ já trata `OperationCanceledException` com `RequestAborted`); remover ou ajustar o `backend.md`
- [Fase 04] Índice único (`userid`, `name`) em `city`: duas buscas simultâneas da mesma cidade nova gravam duplicata
- [Fase 05] Timeout por check nos health checks (`AddDbContextCheck`/`ProviderHealthCheck`, ~3 s) para os probes do compose
- [Fase 06] Log forging: `cityName` interpolado nos logs do Provider (`CityImageAppService`, `WeatherCache`) — resolver com Serilog/sink JSON; não desligar a redação de URI do `HttpClient` (chave da OpenWeather em `appid=`)
- [Fase 08] 400 automático expõe mensagens do System.Text.Json em inglês e nomes de tipo .NET em `errors` — desligar `AllowInputFormatterExceptionMessages` ou mapear para "JSON inválido."
