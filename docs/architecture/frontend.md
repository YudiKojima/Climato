# Frontend — Arquitetura e Padrões

Vale para `web/`. Seções marcadas **(a partir da Fase NN)** descrevem padrões decididos que ainda não estão no código; ao implementá-los, remover a marcação.

## Stack

React 19 · Vite 6 · JavaScript (JSX) · Ant Design 5 + `@ant-design/icons` · React Router 7 · axios · Vitest + Testing Library (jsdom) · ESLint 9 + Prettier · npm (`package-lock.json` versionado; `npm ci` no CI e na imagem).

Decisão de manter Ant Design e JavaScript: [ADR 0002](decisions/0002-frontend-ant-design-modernizado.md).

## Estrutura de pastas

```
web/src/
├── main.jsx         ← AppProviders + AppRouter; importa antd/dist/reset.css
├── app/             ← AppProviders, AuthProvider/AuthContext, router, ProtectedRoute, GuestRoute
├── layouts/         ← AppLayout (header, conteúdo, footer), AuthLayout (tela dividida)
├── pages/           ← páginas finas: compõem componentes e chamam hooks
├── components/      ← reutilizáveis, cada um com seu .module.css quando precisar
├── hooks/           ← useAsync, useCities, useFavorites, useWeather, useForecast, useCitySearch, useApiFeedback
├── services/        ← client.js (axios) + auth.js, cities.js, weather.js, locations.js, errors.js, session.js
├── theme/           ← tokens.js (claro/escuro), ThemeProvider/ThemeContext, themeStorage.js
├── utils/           ← funções puras (formatTime, formatWeekday, weatherIconUrl)
├── test/setup.js    ← jest-dom, matchMedia, limpeza entre testes
└── assets/
```

## Rotas

| Rota | Página | Protegida |
|---|---|---|
| `/signin`, `/signup` | SignIn, SignUp | Não |
| `/p` | Home (busca + histórico) | Sim |
| `/p/weather/:cityName` | Detalhes do clima | Sim |
| `/p/favorites` | Favoritos | Sim |

Proteção de rota **só** no router — páginas não redirecionam por estado de auth. As rotas são aninhadas com `<Outlet/>`: `/signin` e `/signup` sob `GuestRoute` (quem tem sessão vai para `/p`) e `AuthLayout`; `/p/*` sob `ProtectedRoute` e `AppLayout`. Sair é o item do menu do usuário (`signOut()` do `AuthProvider`); não há rota `/signout`.

## Chamadas à API

- Toda chamada passa por `services/`: `client.js` (instância axios) + um arquivo por recurso (`auth.js`, `cities.js`, `weather.js`); páginas e componentes nunca usam `axios` direto.
- URL base de `import.meta.env.VITE_API_URL`. `web/.env.example` versionado, `web/.env` local ignorado e obrigatório (ver [`runbook.md`](../runbook.md#2-segredos-user-secrets)).
- Na imagem Docker o `VITE_API_URL` é fixado no build (`--build-arg`), não em runtime; o web é servido por nginx com fallback de SPA (`web/nginx.conf`).
- Instância única do axios com interceptor de request (`Authorization: Bearer <token>`) e de response (401 → limpa sessão e vai para `/signin`). `userId` nunca é enviado pelo front.
- Sessão só via `services/session.js` (`saveSession`, `getToken`, `clearSession`, `hasValidSession`); nenhum outro arquivo usa `localStorage` para auth. O nome do usuário vem de `getUserName()`, exposto pelo `AuthProvider` (`useAuth()`).
- Erros: `services/errors.js` — `getApiErrorMessage(err, fallback)`, `getCorrelationId(err)` (header `X-Correlation-ID`) e `isUnexpectedError(err)` (sem resposta ou status ≥ 500). Em ação, `useApiFeedback()` decide: erro de negócio → `message.error` só com a mensagem; erro inesperado → `notification.error` com o código (`CorrelationId`). Em carga de tela, `ErrorState` faz o mesmo.
- Parâmetros de rota/query com `encodeURIComponent`.
- Erros da API chegam como `ProblemDetails`: exibir `title`/`detail`, não a mensagem do axios.
- Busca de cidades (`GET /locations?query=`, mínimo 2 caracteres) é ao confirmar — o geocoding casa o nome inteiro, não prefixo: `CitySearch` + `useCitySearch` mostram até 5 resultados para escolher (setas/Enter/Esc); a escolha navega só pelo nome (homônimos caem na primeira cidade).
- Previsão (`GET /forecasts/{cidade}`) com `useForecast` em seção própria (`ForecastSection`, carregada com `React.lazy`): falha da previsão não derruba o detalhe.

## Tema e estilo

- Tokens do AntD em `theme/tokens.js` aplicados via `ConfigProvider` (`token` + `algorithm` `defaultAlgorithm`/`darkAlgorithm`). Cores, fontes, raios e sombras **só** via token — cores literais só em `theme/tokens.js`.
- `ConfigProvider` com `cssVar: true`: os `.module.css` usam `var(--ant-*)` (ex.: `--ant-color-primary`, `--ant-padding-lg`, `--ant-color-bg-mask`). Para vencer o seletor do AntD em `Typography`, aninhar a classe (`.card .title`), sem `!important`.
- Preferência claro/escuro em `localStorage` (`climato.theme`, via `theme/themeStorage.js`), gravada só quando o usuário alterna; sem escolha, segue `prefers-color-scheme`. `useThemeMode()` → `{ mode, toggleMode }`.
- Layout responsivo com `Grid`/`Flex` do AntD e breakpoints do tema; sem larguras fixas em px para containers.
- Estilo de componente em `.module.css`; `style={{}}` inline só para valor dinâmico (ex.: imagem de fundo).
- Feedback via `App.useApp()` (`message`, `notification`, `modal`) — os estáticos do `antd` não herdam o tema; nunca `alert()`/`confirm()`.
- Carregamento com `Skeleton` no formato do conteúdo; spinner só para ações curtas.
- Gráficos com Recharts; cores só de `theme.useToken()` (nada de hex nas props). Todo gráfico tem `role="img"` com `aria-label` e uma leitura em texto ao lado (ex.: cards da previsão). Dependência pesada de uma tela só entra com `React.lazy`, dentro de `Suspense` (skeleton) e de `ErrorBoundary` (`components/ErrorBoundary.jsx`) com um `ErrorState` — falha ao baixar o chunk não derruba a página.

## Estado

- Estado de tela fica na página (ou em hook da página).
- Carga de tela com `useAsync(fn)` → `{ data, isLoading, error, reload }`; `fn` estável (função de módulo ou `useCallback`), e trocar `fn` descarta a resposta da anterior. Lista mostra skeleton só sem dados (`isLoading && !data`), para o `reload` não piscar.
- Contexto global só para o que é realmente global: sessão (auth) e tema.

## Componentização

- JSX repetido em duas páginas → componente em `components/`.
- Header e footer vêm do layout, não de cada página.

## Testes

Vitest + Testing Library (jsdom), `*.test.js(x)` ao lado do código. Toda lógica em `services/`, `hooks/`, `theme/` e `utils/` nasce com teste; componentes visuais não têm teste por ora. Hooks que só ligam um serviço ao `useAsync` dispensam teste próprio.

## Lint, testes e build

```bash
npm --prefix web run lint
npm --prefix web test
npm --prefix web run build
```

## Ao concluir

- Padrão novo → esta página + linha nova no [`review-checklist.md`](review-checklist.md).
