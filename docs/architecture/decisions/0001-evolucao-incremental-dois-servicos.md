# 0001 — Evolução incremental mantendo dois serviços

**Data:** 2026-10-16
**Status:** Aceito
**Fase:** 00-roteiro-portfolio

## Contexto

O Climato nasceu como trabalho de faculdade sobre microsserviços: um Gateway (usuários, cidades, favoritos, PostgreSQL) e um Provider (OpenWeather + Pexels). Para virar portfólio de backend .NET, havia três caminhos: consolidar o que existe, adicionar um API Gateway real (YARP) separando Identity e Cities, ou ir além com mensageria (RabbitMQ). Também havia a opção de reescrever do zero com um template moderno.

## Decisão

Manter **dois serviços** com comunicação **HTTP síncrona** e evoluí-los **incrementalmente**, uma fase por PR, investindo em profundidade (segurança, resiliência, cache, testes, observabilidade, containers) em vez de mais serviços.

## Alternativas consideradas

- **YARP + Identity/Cities separados** — mais "microsserviços" no papel, mas multiplica bancos, deploys e configuração para um domínio pequeno; o ganho de aprendizado demonstrável é menor que o custo.
- **Mensageria (RabbitMQ/MassTransit)** — chama atenção em entrevista, mas o domínio não tem fluxo assíncrono natural; seria complexidade inventada.
- **Reescrever com template** — código bonito desde o início, mas perde o histórico de evolução e corre o risco de parecer template copiado.

## Consequências

- O histórico do git conta a evolução ("encontrei A1, corrigi assim"), material para entrevista.
- A divisão Gateway (estado, identidade) × Provider (integração externa, cache) precisa ser defendida — ver [`system-design.md`](../system-design.md).
- YARP, separação de serviços e mensageria entram no README como "próximos passos", não no escopo.
- Padrões de microsserviços demonstrados: BFF, camada anticorrupção, resiliência entre serviços, cache, health checks, correlation ID, um banco por serviço (o Provider não tem banco).
