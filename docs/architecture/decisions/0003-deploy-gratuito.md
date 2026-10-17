# 0003 — Docker Compose local + deploy público em camadas gratuitas

**Data:** 2026-10-16
**Status:** Aceito
**Fase:** 00-roteiro-portfolio

## Contexto

Quem avalia o portfólio é de dois tipos: recrutador (quer clicar num link) e pessoa técnica (quer clonar e rodar). O projeto não tem custo de hospedagem hoje e não deve passar a ter.

## Decisão

1. **`docker compose up`** sobe tudo localmente (gateway, provider, postgres, redis, web) — Fase 05.
2. **Demo pública em camadas gratuitas** — Fase 08. Proposta inicial (confirmada no brainstorming da Fase 08, conforme as ofertas vigentes): APIs em **Render** ou **Fly.io**, PostgreSQL no **Neon**, Redis no **Upstash**, web na **Vercel**.

## Alternativas consideradas

- **Só local** — sem custo nem manutenção, mas recrutador não técnico não vê o projeto funcionando.
- **Azure Container Apps / AWS** — pesa mais no currículo, mas gera custo e exige cuidado contínuo com billing.

## Consequências

- Imagens Docker são o artefato de deploy: o mesmo Dockerfile serve ao compose e à hospedagem.
- Configuração 100% por variáveis de ambiente; nenhum segredo no repositório.
- Camadas gratuitas têm *cold start* (primeira requisição lenta) — o front precisa de um estado de carregamento decente e o README avisa.
- Cotas da OpenWeather (60 req/min) e Pexels (200 req/h) exigem cache no Provider e, na demo, limite de requisições.
