# 0002 — Manter Ant Design e JavaScript no frontend, com tema próprio

**Data:** 2026-10-16
**Status:** Aceito
**Fase:** 00-roteiro-portfolio

## Contexto

O frontend funciona, mas é visualmente simples: largura fixa, estilos inline, `alert()`, aparência padrão do Ant Design. O foco do portfólio é backend, mas um visual moderno melhora a primeira impressão de quem abre a demo.

## Decisão

Manter **React + Vite + Ant Design em JavaScript** e modernizar com um **tema próprio** via `ConfigProvider` (tokens, modo escuro), layout responsivo, CSS Modules, skeletons e telas redesenhadas (Fase 07).

## Alternativas consideradas

- **Tailwind + shadcn/ui + TypeScript** — visual mais "de produto" e TypeScript mostra maturidade, mas exige reescrever todas as telas, desviando esforço do foco (backend).
- **Só migrar para TypeScript** — benefício pequeno para um front de 6 telas cujo foco não é o front.

## Consequências

- Reaproveita componentes e telas existentes; esforço vai para design (tokens, layout, estados) e não para infraestrutura de UI.
- Risco de "cara de painel administrativo": mitigado com tipografia própria, tema customizado, imagens de fundo e layout autoral nas telas principais.
- Cores e fontes só via tokens do tema (regra FE-06 do checklist).
