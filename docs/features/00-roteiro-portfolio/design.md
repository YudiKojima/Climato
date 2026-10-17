# Design — Fase 00: Roteiro do portfólio

**Data:** 2026-10-16
**Fase:** 00
**Status:** Aprovado
**Base:** [`auditoria-inicial.md`](auditoria-inicial.md) — códigos `A1`…`A27`.

---

## Objetivo

Transformar o Climato, um projeto de faculdade sobre microsserviços, em uma peça de portfólio para **vagas de backend .NET**. Ao final, um recrutador deve conseguir:

1. abrir um **link público** e usar o app;
2. clonar o repositório e subir tudo com **`docker compose up`**;
3. ler no README **o que foi feito e por quê** (arquitetura, decisões, testes).

## Decisões

| Tema | Decisão | Alternativas descartadas |
|---|---|---|
| Foco | Backend .NET — arquitetura, segurança, testes, padrões de microsserviços | Full-stack; backend + DevOps pesado |
| Escopo de microsserviços | **Consolidar os 2 serviços atuais** (Gateway/BFF + Provider) com HTTP síncrono ([ADR 0001](../../architecture/decisions/0001-evolucao-incremental-dois-servicos.md)) | API Gateway YARP + separação em Identity/Cities; mensageria (RabbitMQ) |
| Estratégia | **Evolução incremental**, uma fase = um PR, projeto apresentável ao fim de cada fase | Reescrita com template; só o essencial |
| Demonstração | `docker-compose` local **+ deploy público gratuito** ([ADR 0003](../../architecture/decisions/0003-deploy-gratuito.md)) | Só local; cloud paga (Azure/AWS) |
| Idioma | **Português** em código de domínio, mensagens, commits, docs e README | Inglês; bilíngue |
| Frontend | **Manter Ant Design**, modernizado ao máximo com tema próprio ([ADR 0002](../../architecture/decisions/0002-frontend-ant-design-modernizado.md)) | Tailwind + shadcn/ui + TypeScript |
| Runtime | Migrar para **.NET 10 (LTS)** na Fase 01 ([ADR 0004](../../architecture/decisions/0004-dotnet-10.md)) | Ficar no .NET 8 (fim de suporte em 2026-11-10) |
| Novas funcionalidades | Previsão de 5 dias, autocomplete de cidade, tema claro/escuro | — |

**Fora do escopo** (citado no README como "próximos passos"): YARP, separação de serviços, mensageria, Kubernetes, cloud paga, TypeScript no front.

---

## Fases

Cada fase passa pelo fluxo do [`CLAUDE.md`](../../../CLAUDE.md): `/brainstorming` → `design.md` → `/writing-plans` → `plan.md` → implementação → PR. O que está abaixo é o **escopo de entrada** de cada brainstorming, não o design final.

### 01 — Higiene e fundação
Deixar o projeto compilável a partir de um clone e corrigir os bugs estruturais.
- .NET 10 nos dois serviços; solução única `Climato.sln` na raiz (A22)
- `.gitignore` corrigido: versionar `.sln`, `appsettings.json` (sem segredos) e `launchSettings.json` (A23)
- `AddDbContext` + connection string em configuração; repositórios recebem o contexto via DI (A5, A10)
- EF Core Migrations no lugar do script SQL (A9)
- Fim do `async void` (A8); URL do Provider em configuração (A11); remover `WeatherProviderApp.Infrastructure` órfão (A12); namespaces (A13); registros duplicados e pacotes no projeto certo (A20, A21)
- Segredos locais via `dotnet user-secrets` ou `appsettings.Development.json` ignorado

### 02 — Segurança
- Hash de senha (`PasswordHasher<T>` do ASP.NET Identity ou BCrypt — decidir no brainstorming) (A1)
- `POST /auth/register` e `POST /auth/login` emitindo **JWT**; `UserDto` sem senha (A2, A3)
- `[Authorize]` em cidades/clima; `userId` sempre das claims; verificação de dono (A4)
- CORS por configuração, ordem do pipeline corrigida (A6)
- FluentValidation nos DTOs de entrada; erros em **ProblemDetails**; middleware global de exceções
- Front: login/cadastro novos, token no `localStorage`, interceptor do axios com `Authorization` e logout em 401, `VITE_API_URL` (A7, A27 parcial); regra de exclusão de favorita no backend (A18)

### 03 — Comunicação entre serviços
- **Typed HttpClient** com `BaseAddress` configurável e **DTOs tipados** com `System.Text.Json` no lugar do `JsonDictionary` (A19)
- Resiliência com `Microsoft.Extensions.Http.Resilience` (retry, timeout, circuit breaker) nas chamadas Gateway→Provider e Provider→APIs externas
- Erros mapeados corretamente: cidade inexistente = 404; falha externa = 502/503; imagem do Pexels é opcional (A14, A15)
- **Cache** no Provider com `IDistributedCache` (Redis; memória em dev), TTL ~10 min por cidade normalizada
- **Health checks** (`/health/live`, `/health/ready` com Postgres, Redis e Provider)
- Unidades e fusos corretos: vento em km/h, nascer/pôr do sol no horário local da cidade (A16, A17)
- Novos endpoints: **previsão de 5 dias** e **busca de cidades (geocoding)** no Provider, expostos pelo Gateway

### 04 — Testes
- `tests/` com projetos xUnit por serviço: unitários (NSubstitute, FluentAssertions ou Shouldly) e integração (`WebApplicationFactory` + **Testcontainers** Postgres/Redis; **WireMock.Net** simulando OpenWeather/Pexels/Provider) (A24)
- Cobertura dos fluxos críticos: auth, dono do recurso, toggle de favorito, mapeamento de erros, cache
- A partir daqui, TDD é a regra para código novo

### 05 — Containers e CI
- Dockerfile multi-stage por serviço; web servido por nginx (A23)
- `docker-compose.yml`: gateway, provider, postgres, redis, web; `.env.example`; migrations aplicadas na subida
- **GitHub Actions**: build + testes .NET, lint + build do web, build das imagens em todo PR (A25)

### 06 — Observabilidade
- **Serilog** com logs estruturados (console JSON) e enriquecimento
- **Correlation ID** gerado no Gateway e propagado ao Provider via header
- Opcional: OpenTelemetry (traces HTTP + EF Core) com Jaeger no compose

### 07 — Frontend moderno
- Tema próprio via `ConfigProvider` (tokens de cor, tipografia, raio, sombra) e **modo claro/escuro** persistido
- `AppLayout` compartilhado (header, footer), layout responsivo, CSS Modules no lugar de estilos inline (A27)
- Home com busca + **autocomplete**; cards de cidade com temperatura
- Detalhes: imagem da cidade com overlay, métricas em cards com ícones, **previsão de 5 dias** com gráfico
- Skeletons, `message`/`notification` no lugar de `alert()`, estados vazios, microanimações
- Login/cadastro em layout dividido

### 08 — Vitrine e deploy
- Deploy: APIs em Render ou Fly.io, Postgres no Neon, Redis no Upstash, web na Vercel ([ADR 0003](../../architecture/decisions/0003-deploy-gratuito.md))
- README: pitch, link da demo, GIF, diagrama de arquitetura, stack, decisões (links para ADRs), como rodar, testes, próximos passos (A26)
- Usuário de demonstração e limites para proteger as cotas da OpenWeather/Pexels

## Dependências entre fases

```
01 ──▶ 02 ──▶ 03 ──▶ 04 ──▶ 05 ──▶ 06 ──▶ 08
                └────────────────▶ 07 ──┘
```

- 02 antes de qualquer exposição pública (senhas em texto puro).
- 04 depois da 03 para não testar código que vai ser reescrito; testes de segurança podem nascer na 02.
- 07 depende dos endpoints da 03 (previsão e autocomplete); pode correr em paralelo com 04–06.
- 08 depende de 05 (imagens) e 07 (visual).

## Critério de conclusão do roteiro

- Todos os códigos da auditoria com ✅ (ou justificados como não aplicáveis).
- `docker compose up` sobe tudo num clone limpo; CI verde no `main`.
- Demo pública funcionando e linkada no README.
