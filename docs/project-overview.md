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
WeatherGatewayApp/   ← BFF: usuários, cidades, favoritos (EF Core + PostgreSQL)
WeatherProviderApp/  ← integração OpenWeather + Pexels (sem estado)
web/                 ← React 19 + Vite + Ant Design
docs/                ← ver docs/README.md
```

## Fluxo de Trabalho

Definido no [`CLAUDE.md`](../CLAUDE.md): brainstorming → `design.md` → `plan.md` → aprovação → branch `dev/...` → implementação → verificação → `/revisar-padrao` → PR. Ao concluir uma fase, marcar o título abaixo com `✅ Concluída (AAAA-MM-DD)` e os códigos resolvidos na [auditoria](features/00-roteiro-portfolio/auditoria-inicial.md).

---

## Estado Atual (2026-10-16)

**Funciona:** cadastro e login (inseguros — ver A1–A4), busca de clima atual com imagem da cidade, histórico de cidades por usuário, favoritar/desfavoritar, remover cidade.

**Não há:** autenticação real, testes, Docker, CI, logs estruturados, deploy. Um clone do repositório não compila (A23).

**Stack:** .NET 8 · ASP.NET Core Web API · EF Core 9 + Npgsql · AutoMapper · Newtonsoft.Json · React 19 · Vite 6 · Ant Design 5 · axios.

---

## Fases

#### Fase 00 — Roteiro do portfólio ✅ Concluída (2026-10-16)
Brainstorming do roteiro, auditoria inicial (A1–A27), estrutura de documentação, ADRs 0001–0004.
Design: [`features/00-roteiro-portfolio/design.md`](features/00-roteiro-portfolio/design.md)

#### Fase 01 — Higiene e fundação
.NET 10, solução única, `.gitignore`, DbContext via DI, configuração, EF Migrations, correção de bugs estruturais.
Auditoria: A5, A8–A13, A20–A23.

#### Fase 02 — Segurança
Hash de senha, JWT, autorização por dono, CORS, validação, ProblemDetails; front adaptado ao token.
Auditoria: A1–A4, A6, A7, A18, A27 (URL da API).

#### Fase 03 — Comunicação entre serviços
Typed HttpClient + DTOs, resiliência, cache Redis, health checks, previsão de 5 dias, geocoding.
Auditoria: A14–A17, A19.

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

- (nenhuma)
