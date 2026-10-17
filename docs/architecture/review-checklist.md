# Checklist de Revisão de Padrões

Fonte das regras aplicadas pela skill `/revisar-padrao`. Cobre o que exige **julgamento**; regras mecânicas são do lint/compilador.

**Severidades**
- 🔴 **Bloqueante** — não commitar até corrigir ou o usuário liberar explicitamente o item.
- 🟡 **Aviso** — reportar; o usuário decide.
- 🔵 **Sugestão** — opcional.

**Vigência:** a coluna **Desde** indica a fase a partir da qual a regra é cobrada. Regras de fases futuras **só se aplicam se a fase já começou** (pasta `docs/features/NN-*/` existe) ou se o diff introduz aquele assunto.

**Manutenção:** padrão novo em `backend.md` / `frontend.md` → nova linha aqui (GE-01). IDs nunca são reutilizados.

## Backend

| ID | Sev. | Desde | Regra | Como verificar | Por quê |
|---|---|---|---|---|---|
| BE-01 | 🔴 | 00 | Segredo no código ou em arquivo versionado | Chave de API, senha, connection string com senha, `Jwt:SecretKey` em `.cs`, `appsettings.json` ou script | [Configuração e segredos](backend.md#configuração-e-segredos) |
| BE-02 | 🔴 | 00 | `async void` / bloqueio síncrono | `async void` fora de event handler; `.Result`, `.Wait()`, `GetAwaiter().GetResult()` | [Geral](backend.md#geral) |
| BE-03 | 🔴 | 00 | Dependência criada com `new` | `new ConnectionContext()`, `new HttpClient()` ou serviço instanciado em vez de injetado | [Repositórios](backend.md#repositórios-infrastructure) |
| BE-04 | 🔴 | 00 | DTO expõe dado sensível | DTO de resposta com `Password`, hash, token interno | [DTOs](backend.md#dtos-application) |
| BE-05 | 🔴 | 00 | Entidade do Domain retornada pelo controller | Action retornando tipo de `*.Domain` | [DTOs](backend.md#dtos-application) |
| BE-06 | 🔴 | 00 | Camada violada | Domain referenciando outro projeto/pacote de infra; Application referenciando Infrastructure; EF Core fora da Infrastructure | [Dependências entre camadas](backend.md#dependências-entre-camadas) |
| BE-07 | 🔴 | 00 | URL ou chave literal | `"https://..."` de serviço/API externa ou chave no código em vez de configuração | [Comunicação HTTP](backend.md#comunicação-http) |
| BE-08 | 🔴 | 01 | Mudança de entidade sem migration | Entidade ou `OnModelCreating` alterado sem nova migration no mesmo diff | [Migrations](backend.md#migrations) |
| BE-09 | 🔴 | 02 | `userId` vindo do cliente | Parâmetro `userId` em rota, query ou corpo de endpoint autenticado | [CLAUDE.md — O que evitar](../../CLAUDE.md#o-que-evitar) |
| BE-10 | 🔴 | 02 | Recurso sem verificação de dono | Operação sobre `City` por `id` sem filtrar pelo usuário das claims | [Repositórios](backend.md#repositórios-infrastructure) |
| BE-11 | 🔴 | 02 | Endpoint sem `[Authorize]` | Controller/action nova sem `[Authorize]` e sem `[AllowAnonymous]` explícito | [Controllers](backend.md#controllers) |
| BE-12 | 🟡 | 00 | Regra de negócio fora da entidade | AppService alterando estado de entidade campo a campo em vez de chamar método dela | [Entidades](backend.md#entidades-domain) |
| BE-13 | 🟡 | 00 | Query sem escape | Interpolação de entrada do usuário em URL sem `Uri.EscapeDataString` | [Comunicação HTTP](backend.md#comunicação-http) |
| BE-14 | 🟡 | 00 | Mensagem fora do português | Mensagem de exceção ou de erro ao usuário em outro idioma | [Geral](backend.md#geral) |
| BE-15 | 🟡 | 01 | `CancellationToken` não propagado | Método async novo de controller/AppService/repositório/cliente HTTP sem `CancellationToken` | [Geral](backend.md#geral) |
| BE-16 | 🟡 | 02 | Erro fora de `ProblemDetails` | `return BadRequest("texto")`/`NotFound("texto")` com string solta | [Erros](backend.md#erros) |
| BE-17 | 🟡 | 03 | JSON externo sem tipo | `Dictionary<string, object>`, `JObject`, `dynamic` para ler resposta de API | [Comunicação HTTP](backend.md#comunicação-http) |
| BE-18 | 🔴 | 04 | Regra nova sem teste | AppService/entidade/endpoint novo ou alterado sem teste correspondente em `tests/` | [CLAUDE.md — Skills](../../CLAUDE.md#skills-disponíveis) |
| BE-19 | 🟡 | 01 | Mapeamento fora de `Mappings/` | `.Adapt(`, `TypeAdapterConfig` ou mapeamento manual campo a campo de entidade → DTO fora de `Application/Mappings/` e `DependencyInjection.cs` | [Mapeamento](backend.md#mapeamento-mapster) |
| BE-20 | 🔴 | 01 | Options sem validação | Classe `*Options` registrada sem `ValidateOnStart()` ou chave obrigatória sem `[Required]` | [Injeção de dependência por camada](backend.md#injeção-de-dependência-por-camada) |
| BE-21 | 🟡 | 01 | Versão de pacote no `.csproj` | `PackageReference` com atributo `Version` (versões só em `Directory.Packages.props`) | [Dependências entre camadas](backend.md#dependências-entre-camadas) |

## Frontend

| ID | Sev. | Desde | Regra | Como verificar | Por quê |
|---|---|---|---|---|---|
| FE-01 | 🔴 | 00 | `axios` fora de `services/` | Import de `axios` em página ou componente | [Chamadas à API](frontend.md#chamadas-à-api) |
| FE-02 | 🔴 | 02 | URL da API literal | `http(s)://` de API no código em vez de `VITE_API_URL` | [Chamadas à API](frontend.md#chamadas-à-api) |
| FE-03 | 🔴 | 02 | `userId` enviado pelo front | `userId` em query/corpo de chamada | [Chamadas à API](frontend.md#chamadas-à-api) |
| FE-04 | 🟡 | 00 | Redirect de auth na página | `navigate("/signin")` por estado de sessão dentro de página | [Rotas](frontend.md#rotas) |
| FE-05 | 🟡 | 00 | `alert()`/`confirm()` | Uso de diálogos nativos do navegador | [Tema e estilo](frontend.md#tema-e-estilo-a-partir-da-fase-07) |
| FE-06 | 🟡 | 07 | Cor ou fonte literal | `#hex`, `rgb(`, `font-family` em componente em vez de token do tema | [Tema e estilo](frontend.md#tema-e-estilo-a-partir-da-fase-07) |
| FE-07 | 🟡 | 07 | Estilo inline estático | `style={{}}` com valores fixos (não dinâmicos) | [Tema e estilo](frontend.md#tema-e-estilo-a-partir-da-fase-07) |
| FE-08 | 🟡 | 07 | Largura fixa de container | `width: <n>px` em container de página | [Tema e estilo](frontend.md#tema-e-estilo-a-partir-da-fase-07) |
| FE-09 | 🟡 | 00 | Reutilizável inline | JSX repetido em duas páginas (header, card, estado vazio) | [Componentização](frontend.md#componentização) |
| FE-10 | 🟡 | 00 | Erro de carga engolido | Fetch de tela sem tratamento de erro (vira loading infinito ou lista vazia silenciosa) | [Chamadas à API](frontend.md#chamadas-à-api) |

## Geral

| ID | Sev. | Desde | Regra | Como verificar | Por quê |
|---|---|---|---|---|---|
| GE-01 | 🔴 | 00 | Padrão novo sem doc | Convenção nova sem atualizar `backend.md`/`frontend.md` e este checklist | [CLAUDE.md — Manutenção](../../CLAUDE.md#manutenção) |
| GE-02 | 🔴 | 00 | Escopo de outra fase | Mudança que pertence a outra fase do roteiro sem acordo do usuário | [CLAUDE.md — Durante a implementação](../../CLAUDE.md#durante-a-implementação) |
| GE-03 | 🟡 | 00 | Comentário do "o quê" | Comentário que repete o que o código faz | [CLAUDE.md — O que evitar](../../CLAUDE.md#o-que-evitar) |
| GE-04 | 🟡 | 00 | Erro para cenário impossível | `try/catch` ou checagem para condição que o tipo/fluxo já impede | [CLAUDE.md — O que evitar](../../CLAUDE.md#o-que-evitar) |
| GE-05 | 🟡 | 00 | Plano sem checkboxes | Execução de `docs/features/*/plan.md` com passos feitos e checkboxes desmarcados | [CLAUDE.md — Fluxo](../../CLAUDE.md#skills-disponíveis) |
| GE-06 | 🟡 | 00 | Fase fechada sem ✅ | Commit final de fase sem `✅ Concluída (AAAA-MM-DD)` no `project-overview.md` e sem os códigos `A*` marcados na auditoria | [CLAUDE.md — Manutenção](../../CLAUDE.md#manutenção) |
| GE-07 | 🟡 | 00 | Como rodar mudou sem runbook | Mudança de porta, variável, comando ou dependência local sem atualizar `runbook.md` | [CLAUDE.md — Manutenção](../../CLAUDE.md#manutenção) |
| GE-08 | 🔵 | 00 | Escopo extrapolado | Refatoração ou abstração não pedida pela tarefa | [CLAUDE.md — O que evitar](../../CLAUDE.md#o-que-evitar) |
