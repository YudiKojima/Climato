# Design — Fase 04: Testes

**Data:** 2026-10-26 (escopo reduzido em 2026-10-27)
**Fase:** 04
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#04--testes) · [`backend.md`](../../architecture/backend.md) · [auditoria](../00-roteiro-portfolio/auditoria-inicial.md) (A24)
**Branch:** `dev/testes`

---

## Objetivo

Criar os projetos de **testes unitários** dos dois serviços e cobrir o comportamento atual dos fluxos críticos (dono do recurso, favoritos, auth, mapeamento de erros 404/503/499, cache, agregação da previsão), para que as próximas fases mudem código com rede de segurança. A partir desta fase, **TDD é regra**: regra nova nasce com teste (BE-18). Resolve A24.

**Escopo reduzido (2026-10-27):** para acelerar a conclusão do roteiro, a fase ficou só com unitários. O design completo da integração (abaixo, em [Testes de integração (adiados)](#testes-de-integração-adiados)) e o plano pronto ([`integracao-plan.md`](integracao-plan.md)) vão para o backlog, assim como a correção da resposta incompleta da OpenWeather que seria a 04b.

**Fora do escopo:**
- Testes de integração (Testcontainers + WireMock) → `[Backlog]`
- Correção da resposta incompleta da OpenWeather e demais pendências `[Fase 04]` → `[Backlog]`
- CI rodando os testes → Fase 05
- Cobertura de código (Coverlet/relatório) e mutação; testes do front

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Escopo | **Só unitários**; integração e correções em backlog | Fase completa (04a infra + integração, 04b correção com TDD); pular a fase inteira |
| Pendências `[Fase 04]` | Todas re-rotuladas **`[Backlog]`** no `project-overview.md` | Corrigir alguma nesta fase |
| Framework | **xUnit v3** (runner Microsoft.Testing.Platform) | xUnit v2 |
| Asserções | **Shouldly** (MIT) | FluentAssertions 8 (licença comercial), AwesomeAssertions, `Assert` puro |
| Dublês | **NSubstitute** | Moq |
| Projetos | `Climato.{Gateway,Provider}.UnitTests` (os de integração entram quando o backlog for executado) | Um projeto único com `[Trait]` |
| Código de produção | **Nenhuma mudança em `src/`**: testes de caracterização | Refatorar para facilitar teste |

---

## 1. Estrutura

```
tests/
├── Climato.Provider.UnitTests/  → Provider.Application, Provider.Domain
└── Climato.Gateway.UnitTests/   → Gateway.Api, Gateway.Application, Gateway.Domain
```

- Os projetos entram no `Climato.slnx` numa pasta `/Tests/`.
- Pacotes com versão só no `Directory.Packages.props` (BE-21): `xunit.v3`, `Shouldly`, `NSubstitute`. Versões fixadas no plano.
- Pastas espelham `src/` (ex.: `Application/Services/ForecastAppServiceTests.cs`).
- Nome do teste: `Metodo_Cenario_ResultadoEsperado` (ex.: `GetByCityAsync_FusoNegativo_AgrupaPelaDataLocal`). Corpo em Arrange / Act / Assert, sem comentários marcando as seções.
- Sem rede, Docker ou chave real: toda dependência é substituída com NSubstitute.

## 2. Provider

Dependências substituídas (`IOpenWeatherClient`, `IProviderCache`, `ICityImageAppService`, `IDistributedCache`). Os métodos privados (`ToForecast`, `CreateKey`) são testados pelo comportamento público, sem `InternalsVisibleTo`.

- **`ForecastAppService`** (via `GetByCityAsync`):
  - fuso positivo (+9 h, Tóquio) e negativo (−3 h, São Paulo): pontos agrupados pela **data local**, não UTC;
  - `TempMin`/`TempMax` = mín/máx do dia; descrição e ícone do ponto mais próximo de 12:00 local;
  - `PrecipitationProbability` = maior `pop` × 100 arredondado; `WindSpeedKmh` = maior vento × 3,6 com 1 casa;
  - no máximo 5 dias, em ordem;
  - cache hit não chama o client; `null` do client → `null` e nada gravado; sucesso grava com prefixo `previsao` e 30 min.
- **`WeatherAppService`**: vento em km/h, nascer/pôr do sol no offset da cidade, imagem opcional (`null` do Pexels → `ImageUrl` nulo), imagem buscada pelo nome devolvido pela OpenWeather, cache hit, `null` não grava, sucesso grava `clima`/10 min.
- **`LocationAppService`**: nome de `local_names.pt` com fallback para `name`; lista vazia também vai para o cache (`locais`/24 h).
- **`ProviderCache`** (com `IDistributedCache` substituto, observando a chave recebida):
  - `"  São   Paulo "` → `clima:sao paulo`; o mesmo texto com `previsao` e `locais` gera `previsao:sao paulo` e `locais:sao paulo` — garante que a chave `clima:` não muda;
  - TTL absoluto igual à duração pedida; ida e volta pelo `MemoryDistributedCache` devolve o mesmo conteúdo;
  - exceção do cache na leitura → `null`; na gravação → engolida (log `Warning`);
  - `OperationCanceledException` propaga.

## 3. Gateway

- **Domain:** `City.ToggleFavorite` (ida e volta), `City.EnsureCanBeDeleted` (favorita → `DomainException`), `User.NormalizeEmail` e construtor.
- **`CityAppService`** (dono do recurso e favoritos): cidade de outro usuário/inexistente → `false`/`null` sem chamar `DeleteAsync`/`UpdateAsync`; favorita → `DomainException` e não remove; toggle chama `UpdateAsync` e devolve o DTO.
- **`WeatherAppService`:** Provider `null` → `null` sem tocar no repositório; cidade nova → `CreateAsync` uma vez com o nome devolvido pelo Provider; cidade existente → não cria; previsão repassada.
- **`AuthAppService`:** e-mail já usado → `null`; e-mail normalizado antes da consulta; usuário inexistente ou senha errada → `null`; `SuccessRehashNeeded` → novo hash e `UpdateAsync`.
- **`UserAppService`** e **`LocationAppService`** (query aparada antes do Provider).
- **Validators:** `RegisterPostDtoValidator`, `LoginPostDtoValidator`, `LocationSearchQueryValidator` — casos válidos e mensagens em português dos inválidos.
- **`GlobalExceptionHandler`** (com `DefaultHttpContext` e `IProblemDetailsService` substituto): `DomainException` → 409 com a mensagem; `UserNotFoundException` → 401 "Sessão inválida."; `ProviderUnavailableException` → 503; outra → 500 "Erro interno." sem detalhe; `OperationCanceledException` com `RequestAborted` cancelado → 499 sem corpo. O teste cobre a lógica do handler; **se o ramo 499 é alcançável no pipeline continua em backlog**.

## 4. Documentação

- **`backend.md`:** linha **Testes** da Stack com o que existe (unitários) e o alvo (integração em backlog); nova seção **Testes** (projetos, convenções de nome, NSubstitute, TDD como regra); estrutura de `tests/`.
- **`CLAUDE.md` › Verificação:** acrescentar `dotnet test --solution Climato.slnx`.
- **`runbook.md`:** seção **Testes**.
- **`auditoria-inicial.md`:** A24 ✅ Fase 04 (unitários; integração em backlog).
- **`project-overview.md`:** Fase 04 ✅; pendências re-rotuladas.

## Pendências

Re-rotuladas de `[Fase 04]` para **`[Backlog]`** no `project-overview.md`:

- Resposta 200 incompleta da OpenWeather vira 500 (clima e previsão)
- 400 do Provider para `cityName` só com espaços vira 503 no Gateway
- Índice único (`userid`, `name`) em `city`
- Mapster: `GlobalSettings` → instância própria + `Compile()`
- `ValidationProblemDetails` duplicado entre `ValidationFilter` e `InvalidModelStateResponseFactory`
- Confirmar se o ramo 499 é alcançável

Nova: **`[Backlog]` testes de integração**, com plano em [`integracao-plan.md`](integracao-plan.md).

---

## Testes de integração (adiados)

Desenho aprovado em 2026-10-26, mantido para quando o backlog for executado (plano: [`integracao-plan.md`](integracao-plan.md)).

- Projetos `Climato.{Gateway,Provider}.IntegrationTests` com `WebApplicationFactory<Program>` no ambiente `Testing`, **Testcontainers** (Postgres no Gateway, Redis no Provider) e **WireMock.Net** no lugar de OpenWeather, Pexels e Provider. Precisam de Docker.
- Gateway: um Postgres por assembly (`AssemblyFixture`), host iniciado antes dos testes (migrations), **usuário novo por teste** em vez de limpar o banco; cenários de auth, token inválido/expirado/usuário apagado, dono do recurso, favoritos, clima (404/503 sem retry/Provider fora), previsão, busca e health.
- Provider: factory por classe, cache em memória; cenários de clima (imagem opcional, 404, 503 após retries, JSON inválido, cache), previsão, busca, health e uma classe com Redis real (chave e TTL).
- Nenhum teste chama serviço externo real; delay do retry zerado no teste.
- Regra nova do checklist ao executar: **BE-29** 🔴 — teste dependente de serviço externo real ou de segredo real.
