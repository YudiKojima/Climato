# Documentação — Climato

| Pasta / arquivo | Conteúdo | Ciclo de vida |
|---|---|---|
| [`project-overview.md`](project-overview.md) | Estado do projeto e roteiro das fases | Atualizado ao fim de cada fase |
| [`architecture/`](architecture/) | Padrões vigentes: [`system-design.md`](architecture/system-design.md), [`backend.md`](architecture/backend.md), [`frontend.md`](architecture/frontend.md) | Vivo — atualizado ao adotar novo padrão |
| [`architecture/decisions/`](architecture/decisions/) | ADRs — uma decisão arquitetural por arquivo | Criado na decisão; nunca editado, só substituído |
| [`architecture/review-checklist.md`](architecture/review-checklist.md) | Regras de revisão aplicadas por `/revisar-padrao` | Vivo — linha nova a cada padrão novo |
| [`runbook.md`](runbook.md) | Como subir banco, serviços e web localmente | Vivo |
| [`features/`](features/) | Uma pasta por fase: `design.md` (o quê e por quê) + `plan.md` (passo a passo) | Congelado após a entrega da fase |
| [`assets/`](assets/) | Diagramas e capturas de tela usados no README e nos docs | Referência |

## Fases

| Fase | Pasta |
|---|---|
| 00 — Roteiro do portfólio + auditoria inicial | [`features/00-roteiro-portfolio/`](features/00-roteiro-portfolio/) |
| 01 — Higiene e fundação | [`features/01-higiene-fundacao/`](features/01-higiene-fundacao/) |
| 02 — Segurança | `features/02-seguranca/` |
| 03 — Comunicação entre serviços | `features/03-comunicacao-servicos/` |
| 04 — Testes | `features/04-testes/` |
| 05 — Containers e CI | `features/05-containers-ci/` |
| 06 — Observabilidade | `features/06-observabilidade/` |
| 07 — Frontend moderno | `features/07-frontend-moderno/` |
| 08 — Vitrine e deploy | `features/08-vitrine-deploy/` |

A pasta de cada fase é criada pelo `/brainstorming` no início dela. O escopo resumido de cada uma está no [`project-overview.md`](project-overview.md).

## Convenções

- Nova fase → `features/NN-nome-com-hifens/` com `design.md` e `plan.md`.
- Documento extra na mesma fase → `<assunto>-design.md` ou `<assunto>-plan.md`.
- Não criar `docs/specs/` nem `docs/superpowers/` — esta estrutura substitui ambos.
- Imagens → `assets/screenshots/`; diagramas (fonte `.mmd`/`.drawio` + export) → `assets/diagrams/`.
