# Climato — Instruções para Claude Code

Projeto de portfólio: app de clima em **microsserviços** (.NET + React). O objetivo é demonstrar boas práticas de backend .NET, containers e deploy. Visão geral e roteiro em [`docs/project-overview.md`](docs/project-overview.md).

## Documentos de Referência

| Documento | Quando ler |
|---|---|
| [`docs/architecture/backend.md`](docs/architecture/backend.md) | **Obrigatório** antes de qualquer tarefa em `WeatherGatewayApp/` ou `WeatherProviderApp/` — camadas, convenções de código, decisões técnicas |
| [`docs/architecture/frontend.md`](docs/architecture/frontend.md) | **Obrigatório** antes de qualquer tarefa em `web/` — estrutura, tema do Ant Design, chamadas à API |
| [`docs/architecture/system-design.md`](docs/architecture/system-design.md) | Para entender os serviços, a comunicação entre eles e a arquitetura-alvo |
| [`docs/project-overview.md`](docs/project-overview.md) | Estado atual, fases concluídas e próximas — **não é fonte de padrões de código** |
| [`docs/features/00-roteiro-portfolio/`](docs/features/00-roteiro-portfolio/) | Roteiro aprovado (`design.md`) e auditoria inicial (`auditoria-inicial.md`, códigos `A1`…`A27`) |
| [`docs/architecture/review-checklist.md`](docs/architecture/review-checklist.md) | Regras aplicadas pela skill `/revisar-padrao` |
| [`docs/runbook.md`](docs/runbook.md) | Como subir banco, serviços e web localmente |
| [`docs/README.md`](docs/README.md) | Índice da pasta `docs/` |

---

## Skills Disponíveis

Plugin: **`superpowers`** (`superpowers@claude-plugins-official`).

| Skill | Quando usar |
|---|---|
| `brainstorming` | Início de cada fase ou decisão de design — explora requisitos antes de implementar |
| `writing-plans` | Após o design aprovado — converte o design em plano passo a passo |
| `executing-plans` / `subagent-driven-development` | Executar um plano existente com checkpoints |
| `test-driven-development` | A partir da Fase 04 (projetos de teste existem): toda regra nova nasce com teste |
| `using-git-worktrees` | Ao iniciar uma fase — branch isolado a partir do `main` |
| `systematic-debugging` | Qualquer bug ou comportamento inesperado — antes de propor fix |
| `verification-before-completion` | Antes de declarar fase concluída — rodar os comandos de verificação e confirmar o output |
| `revisar-padrao` (projeto) | **Obrigatória** antes de qualquer commit e de abrir PR |
| `requesting-code-review` | Antes de abrir PR |
| `finishing-a-development-branch` | Implementação completa → commit final e PR |

**Convenção de branch:** `dev/{descricao-com-hifens}` a partir do `main`. Ex.: `dev/higiene-fundacao`, `dev/autenticacao-jwt`.

**Fluxo padrão de uma fase:**
1. `/brainstorming` → design aprovado → `docs/features/NN-nome/design.md`
2. `/writing-plans` → plano aprovado → `docs/features/NN-nome/plan.md`
3. `/using-git-worktrees` → branch `dev/{descricao}`
4. `/executing-plans` → implementação com checkpoints, marcando os checkboxes do plano
5. `/verification-before-completion` → comandos da seção [Verificação](#verificação)
6. `/revisar-padrao` → sem itens 🔴 (ou liberados pelo usuário) → `/requesting-code-review`
7. `/finishing-a-development-branch` → `project-overview.md` com a fase marcada `✅ Concluída (AAAA-MM-DD)` **no mesmo commit final** → PR

**Onde salvar documentos (sobrepõe o caminho padrão das skills):**
- Specs e planos **nunca** em `docs/superpowers/` nem `docs/specs/` — sempre em `docs/features/NN-nome/` (`NN` = número da fase do [`project-overview.md`](docs/project-overview.md)). Documento extra da mesma fase: `<assunto>-design.md` ou `<assunto>-plan.md`.
- Decisão arquitetural → ADR em `docs/architecture/decisions/` (ver o README da pasta).

### Verificação

Enquanto a Fase 01 não unificar a solução, os comandos são por serviço:

```bash
dotnet build WeatherGatewayApp/WeatherGatewayApp/WeatherGatewayApp.csproj
dotnet build WeatherProviderApp/WeatherProviderApp/WeatherProviderApp.csproj
npm --prefix web run lint
npm --prefix web run build
```

Cada fase que mudar isso (solução única na 01, `dotnet test` na 04, `docker compose` na 05) atualiza esta seção e o [`runbook.md`](docs/runbook.md).

---

## Como se Comportar

### Antes de Implementar

1. Backend: ler [`backend.md`](docs/architecture/backend.md). Frontend: ler [`frontend.md`](docs/architecture/frontend.md). Não escrever código sem ter lido.
2. Consultar o [`project-overview.md`](docs/project-overview.md) para saber em que fase o projeto está e o que pertence a ela.
3. Tarefa nova ou com decisão de design → `brainstorming` → `writing-plans` antes de qualquer código.
4. **Aguardar aprovação explícita** antes de escrever código.

### Durante a Implementação

- Implementar **somente o escopo da fase/passo atual**. Achado de outra fase → registrar como pendência no `project-overview.md`, não corrigir.
- Ao corrigir um item da auditoria, citar o código (`A6`, `A12`…) no plano e no PR.
- Verificar o `.csproj` antes de usar namespace de pacote externo.
- Em caso de ambiguidade, parar e confirmar.

### Estilo de Resposta

- Respostas curtas e diretas, em português.
- Referências a código como links clicáveis: `[arquivo.cs](caminho/relativo.cs:42)`.

### Git e Commits

- **Nunca commitar durante a implementação de uma fase.** Só quando o usuário pedir ou ao finalizar com `/finishing-a-development-branch`. O usuário revisa via `git diff`.
- Antes de qualquer commit e PR: `/revisar-padrao`. Com 🔴, não commitar até corrigir ou o usuário liberar o item.
- Mensagens de commit em português, no imperativo: `Adiciona autenticação JWT no Gateway`.
- A partir da Fase 05 (CI), **PR com CI ❌ não é mergeado**.
- **Nunca** commitar segredos (chaves da OpenWeather/Pexels, senha de banco, `Jwt:SecretKey`). Configuração local fica em `appsettings.Development.json` (ignorado) ou user-secrets; o repositório só tem `appsettings.json` sem segredos e `.env.example`.

### O que Evitar

- Refatorar ou criar abstrações além do que a fase exige (YARP, mensageria e novos serviços estão **fora do escopo** — [ADR 0001](docs/architecture/decisions/0001-evolucao-incremental-dois-servicos.md)).
- Tratamento de erro para cenários impossíveis.
- Comentários que descrevem o que o código faz.
- `async void` (exceto event handlers), `.Result`/`.Wait()`.
- `new` de dependências que deveriam vir do DI (DbContext, HttpClient).
- `userId` vindo do cliente (query/corpo) — após a Fase 02, sempre das claims do JWT.

---

## Manutenção

- Novo padrão de backend → atualizar [`backend.md`](docs/architecture/backend.md) e o [`review-checklist.md`](docs/architecture/review-checklist.md).
- Novo padrão de frontend → atualizar [`frontend.md`](docs/architecture/frontend.md) e o checklist.
- Mudou como rodar o projeto → atualizar [`runbook.md`](docs/runbook.md) e a seção [Verificação](#verificação).
- Ao concluir uma fase → marcar `✅ Concluída (AAAA-MM-DD)` no [`project-overview.md`](docs/project-overview.md) **antes do commit final**, e marcar os códigos da auditoria resolvidos em [`auditoria-inicial.md`](docs/features/00-roteiro-portfolio/auditoria-inicial.md).
