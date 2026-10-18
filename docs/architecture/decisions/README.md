# Architecture Decision Records (ADRs)

Cada arquivo registra **uma** decisão arquitetural: contexto, decisão e consequências. Serve para que o "por quê" sobreviva às conversas em que a decisão foi tomada — e, num portfólio, mostra a quem lê como as escolhas foram pensadas.

## Quando criar

Ao tomar uma decisão que afeta mais de um arquivo ou que alguém poderia questionar depois (padrão, biblioteca, modelagem, trade-off aceito).

## Regras

- Nome: `NNNN-titulo-com-hifens.md`, numeração sequencial.
- ADR aceito não é editado. Se a decisão mudar, criar um novo e marcar o antigo como `Substituído por NNNN`.
- Padrões de código resultantes ficam em [`../backend.md`](../backend.md) / [`../frontend.md`](../frontend.md); o ADR guarda só a justificativa.

## Índice

| ADR | Decisão | Status |
|---|---|---|
| [0001](0001-evolucao-incremental-dois-servicos.md) | Evoluir incrementalmente mantendo dois serviços com HTTP síncrono | Aceito |
| [0002](0002-frontend-ant-design-modernizado.md) | Manter Ant Design e JavaScript no frontend, com tema próprio | Aceito |
| [0003](0003-deploy-gratuito.md) | Docker Compose local + deploy público em camadas gratuitas | Aceito |
| [0004](0004-dotnet-10.md) | Migrar de .NET 8 para .NET 10 | Aceito |
| [0005](0005-mapster.md) | Mapster no lugar do AutoMapper | Aceito |
| [0006](0006-jwt-localstorage-sem-refresh.md) | JWT no localStorage, sem refresh token | Aceito |

## Template

```markdown
# NNNN — Título da decisão

**Data:** AAAA-MM-DD
**Status:** Proposto | Aceito | Substituído por NNNN
**Fase:** NN-nome

## Contexto
Qual problema ou força motivou a decisão.

## Decisão
O que foi decidido, em uma ou duas frases diretas.

## Alternativas consideradas
- Alternativa A — por que não.

## Consequências
O que fica mais fácil, o que fica mais difícil, o que passa a ser obrigatório.
```
