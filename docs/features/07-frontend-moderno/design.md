# Design — Fase 07a: Fundação visual

**Data:** 2026-11-05
**Fase:** 07 (bloco **a** de 2)
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#07--frontend-moderno) · [`frontend.md`](../../architecture/frontend.md) · [ADR 0002](../../architecture/decisions/0002-frontend-ant-design-modernizado.md) · auditoria **A27**
**Branch:** `dev/frontend-moderno`

---

## Objetivo

Dar ao `web/` a base visual e estrutural da Fase 07: pastas no alvo do `frontend.md`, tema próprio do Ant Design com modo claro/escuro persistido, `AppLayout` e `AuthLayout` compartilhados, as cinco telas atuais redesenhadas com CSS Modules, skeletons, estados vazios e de erro, e feedback pelo AntD. Resolve o restante do **A27** e a pendência `[Fase 07]` do `handleFavoriteClick`. Introduz testes de lógica no front (Vitest).

**Blocos da Fase 07:**
- **07a — Fundação visual** (este documento).
- **07b — Busca e previsão**: busca de cidades (`GET /locations`, ao confirmar), previsão de 5 dias com gráfico, temperatura nos cards. Design próprio (`busca-previsao-design.md`) depois do merge da 07a.

**Fora do escopo:**
- Busca de cidades, previsão, gráfico e temperatura nos cards → 07b (o `CityDto` não tem temperatura; a decisão de como obtê-la é da 07b).
- Qualquer mudança no backend.
- Testes de componentes visuais (render de telas) → backlog.

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Divisão | Dois blocos, um PR cada (07a fundação, 07b features) | Fase única; três blocos |
| Testes | **Vitest + Testing Library (jsdom)** só para lógica: `session`, `errors`, tema, hooks de carga | Testar render de componentes; sem testes no front |
| Identidade | Tema **Céu**: primária azul-céu, neutros frios, raio 12, sombras suaves; escuro azul-noite | Neutro + acento laranja; cor por clima |
| Tokens no CSS | `theme.cssVar` do `ConfigProvider`; `.module.css` usa só `var(--ant-*)` | `antd-style` (CSS-in-JS, dependência nova); variáveis `--climato-*` duplicadas |
| Header | Avatar com inicial + nome e menu (Favoritos, tema, Sair); `AuthProvider` expõe a sessão; `session.js` passa a guardar `userName` (`auth.user.name`, já devolvido pelo login) | Só ações, sem nome |
| Correlation ID | Mostrado **só em 5xx/erro de rede**, com botão copiar, quando o header `X-Correlation-ID` existir | Em todo erro; não mostrar |
| Feedback | `App.useApp()` (`message`, `notification`, `modal`) — nada de `message` estático | `message` estático (não herda o tema) |

---

## 1. Estrutura de pastas

```
web/src/
├── main.jsx                 ← renderiza <AppProviders><AppRouter/></AppProviders>
├── app/
│   ├── AppProviders.jsx     ← ThemeProvider → ConfigProvider (tema + cssVar + pt_BR) → App do AntD → AuthProvider
│   ├── AuthProvider.jsx     ← contexto { userName, isAuthenticated, signIn(auth), signOut() } sobre session.js
│   ├── router.jsx           ← rotas (ver §3)
│   ├── ProtectedRoute.jsx   ← sem sessão → /signin
│   └── GuestRoute.jsx       ← com sessão → /p (tira o redirect de SignIn/SignUp, FE-04)
├── theme/
│   ├── tokens.js            ← tokens base + claro/escuro (getThemeConfig(mode))
│   ├── ThemeProvider.jsx    ← contexto { mode, toggleMode }
│   └── themeStorage.js      ← lê/grava "climato.theme"; padrão = prefers-color-scheme
├── layouts/
│   ├── AppLayout.jsx + .module.css
│   └── AuthLayout.jsx + .module.css
├── pages/                   ← Home, Favorites, WeatherDetails, SignIn, SignUp (+ .module.css quando precisar)
├── components/              ← CityCard, CityGrid, CityGridSkeleton, EmptyState, ErrorState, UserMenu, ThemeToggle
├── hooks/                   ← useAsync, useCities, useFavorites, useWeather
├── services/
│   ├── client.js            ← instância axios + interceptors (igual ao api.js de hoje)
│   ├── auth.js, cities.js, weather.js
│   ├── errors.js            ← getApiErrorMessage, getCorrelationId, isUnexpectedError
│   └── session.js           ← + userName
└── assets/
```

Somem: `App.jsx`, `App.css`, `services/api.js`, `styles/`, `components/Loading.jsx`, `components/Footer.jsx` (vira parte do `AppLayout`), `pages/SignOut.jsx` e a rota `/signout` (o Sair do menu chama `signOut()`; nada no código navega para `/signout`). `formatDatetime.jsx` vira `utils/formatTime.js` (não é componente).

## 2. Tema

- `tokens.js` exporta `getThemeConfig(mode)` → `{ algorithm, token, cssVar: true }`.
  - Base: `colorPrimary` azul-céu, `borderRadius: 12`, `fontFamily` do sistema, `boxShadow` suave.
  - Claro: `defaultAlgorithm`, fundo `colorBgLayout` levemente azulado.
  - Escuro: `darkAlgorithm`, `colorBgLayout`/`colorBgContainer` azul-noite.
- `themeStorage.js`: `getInitialMode()` (valor salvo válido → ele; senão `matchMedia('(prefers-color-scheme: dark)')`) e `saveMode(mode)`. `localStorage` com `try/catch` (modo privado não quebra a tela).
- `ThemeProvider` guarda o `mode`, `toggleMode()` alterna e persiste. `ThemeToggle` (ícone sol/lua) usa `useThemeMode()`.
- Cores, fontes, raios e sombras **só** via token/`var(--ant-*)` (FE-06). Logo: a versão “sem fundo preto” no claro e “sem fundo” no escuro.

## 3. Rotas e layouts

| Rota | Guard | Layout | Página |
|---|---|---|---|
| `/signin`, `/signup` | `GuestRoute` | `AuthLayout` | SignIn, SignUp |
| `/p` | `ProtectedRoute` | `AppLayout` | Home |
| `/p/weather/:cityName` | `ProtectedRoute` | `AppLayout` | WeatherDetails |
| `/p/favorites` | `ProtectedRoute` | `AppLayout` | Favorites |
| `*` | — | — | `Navigate` → `/signin` |

Rotas aninhadas com `<Outlet/>`: um guard e um layout por grupo, páginas não repetem header/footer (FE-09).

- **AppLayout** (`Layout` do AntD): header fixo com logo (link para `/p`), links Início/Favoritos (em telas `< md` os links vão para o menu do avatar), `ThemeToggle`, `UserMenu` (avatar com inicial, nome em `≥ md`, itens Favoritos e Sair). Conteúdo com `max-width` e padding responsivo. Footer `Climato © {ano}`.
- **AuthLayout**: `Row` em duas colunas em `≥ md` — à esquerda painel com `background.png`, overlay, logo e uma frase curta; à direita o formulário centralizado. Em `< md` só o formulário, com a logo em cima. `ThemeToggle` no canto.

## 4. Dados, carregamento e erros

- **`useAsync(fn, deps)`** → `{ data, isLoading, error, reload }`; ignora resposta de efeito antigo (troca rápida de `cityName`). `useCities`, `useFavorites`, `useWeather(cityName)` são finos sobre ele.
- **Carga de tela** (FE-10):
  - carregando → skeleton no formato do conteúdo (`CityGridSkeleton` com cards; `Skeleton` da imagem + métricas no detalhe);
  - erro → `ErrorState` com a mensagem de `getApiErrorMessage`, botão **Tentar de novo** (`reload`) e, se `isUnexpectedError`, o correlation ID com botão copiar;
  - 404 no clima → `EmptyState` “Cidade não encontrada”, com botão voltar;
  - lista vazia → `EmptyState` (Home: “Pesquise uma cidade para começar”; Favoritos: “Nenhuma favorita ainda”).
  - O `Favorites` hoje engole o erro (só `finally`) — passa a usar o mesmo fluxo.
- **Ações no `CityCard`** (favoritar, remover): `try/catch` nas duas (resolve a pendência); erro de negócio → `message.error`; erro inesperado → `notification.error` com o ID. Remover pede confirmação com `modal.confirm`. Depois da ação o card chama o callback do pai (`reload`).
- **Login/cadastro**: erro → `message.error(getApiErrorMessage(...))`, como hoje; erro inesperado → `notification` com o ID.
- **`errors.js`**:
  - `getApiErrorMessage(error, fallback)` — igual ao atual (primeiro erro de validação → `detail` → `title` → fallback).
  - `getCorrelationId(error)` → `error.response?.headers?.['x-correlation-id']` ou `undefined`.
  - `isUnexpectedError(error)` → sem `response` (rede) ou status `>= 500`.
- `WeatherDetails` continua mostrando o nome de `weather.city` (o clima atual já vem traduzido).

## 5. Telas

- **Home**: título + campo de busca (o mesmo `Input.Search` de hoje, navega para `/p/weather/:nome` ao confirmar — a busca na API é da 07b), seção “Cidades pesquisadas” com `CityGrid`.
- **Favorites**: título + `CityGrid` das favoritas (sem botão remover, já que favorita não pode ser removida).
- **WeatherDetails**: hero com a imagem da cidade (`style` inline só para o `backgroundImage`, valor dinâmico — FE-07), overlay com cidade/país, ícone, temperatura e descrição; métricas em `Card`s com ícones do `@ant-design/icons` num `Row` responsivo (mín/máx, sensação, umidade, pressão, vento, direção, nascer/pôr do sol). Sem imagem → fundo com gradiente do token primário.
- **CityCard**: nome, país, botão de favorito (estrela), botão remover (só se não favorita), hover com leve elevação/translate (`transition` no `.module.css`).
- **SignIn/SignUp**: mesmos campos e validações de hoje, dentro do `AuthLayout`; link entre as duas telas com `Link` do router (sem `<a onClick>` com cor literal).

## 6. Testes

Vitest + `@testing-library/react` + `@testing-library/jest-dom` + `jsdom`, configurados no `vite.config.js` (`test.environment: 'jsdom'`). Script `"test": "vitest run"`. Arquivos `*.test.js(x)` ao lado do código.

| Alvo | Casos |
|---|---|
| `session.js` | salva token/expiração/nome; `hasValidSession` com expiração futura, passada e ausente; `clearSession` limpa tudo |
| `errors.js` | mensagem de validação, `detail`, `title`, fallback; `getCorrelationId` com e sem header; `isUnexpectedError` para rede, 500, 503, 404, 400 |
| `themeStorage.js` | sem valor salvo → segue o sistema (claro/escuro); valor salvo vence; valor inválido é ignorado; `localStorage` que lança não quebra |
| `ThemeProvider` | `toggleMode` alterna e persiste |
| `useAsync` | sucesso, erro, `reload`, resposta antiga descartada |

## 7. Documentação e CI

| Arquivo | Mudança |
|---|---|
| `frontend.md` | Estrutura passa a ser a real (remove “Alvo”); tira o “(a partir da Fase 07)” de Tema e estilo; seções `cssVar`, `App.useApp()`, hooks de carga, correlation ID, testes; rotas sem `/signout` |
| `review-checklist.md` | **FE-12** 🟡 07: lógica nova em `services/`, `hooks/`, `theme/` ou `utils/` sem teste; **FE-13** 🟡 07: `message`/`notification`/`Modal` estáticos em vez de `App.useApp()` |
| `CLAUDE.md` e `runbook.md` | Verificação ganha `npm --prefix web test` |
| `.github/workflows` | Job Web ganha `npm test` depois do lint |
| `project-overview.md` | 07a concluída no fechamento; pendência do `CityCard` removida; A27 marcado na auditoria |

## Riscos

- **`cssVar` e componentes de terceiros**: só o AntD publica as variáveis; nada além dele é usado.
- **Logo em modo escuro**: os PNGs atuais podem não contrastar bem no escuro; se nenhum dos dois servir, usar o texto “Climato” com `Typography` no header escuro (decisão na implementação, sem asset novo).