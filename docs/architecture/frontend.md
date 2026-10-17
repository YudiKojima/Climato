# Frontend — Arquitetura e Padrões

Vale para `web/`. Seções marcadas **(a partir da Fase NN)** descrevem padrões decididos que ainda não estão no código; ao implementá-los, remover a marcação.

## Stack

React 19 · Vite 6 · JavaScript (JSX) · Ant Design 5 + `@ant-design/icons` · React Router 7 · axios · ESLint 9 + Prettier.

Decisão de manter Ant Design e JavaScript: [ADR 0002](decisions/0002-frontend-ant-design-modernizado.md).

## Estrutura de pastas

```
web/src/
├── main.jsx, App.jsx
├── router/          ← index.jsx (rotas) + ProtectedRoute.jsx
├── pages/           ← uma página por rota (Home, WeatherDetails, Favorites, SignIn, SignUp, SignOut)
├── components/      ← componentes reutilizáveis (CityCard, Loading, Footer…)
├── services/api.js  ← todas as chamadas HTTP
├── styles/          ← CSS por tela
└── assets/          ← imagens e logo
```

**Alvo (Fase 07):**

```
web/src/
├── app/             ← providers (ConfigProvider/tema, AuthProvider), router
├── layouts/         ← AppLayout (header, conteúdo, footer), AuthLayout (tela dividida)
├── pages/           ← páginas finas: compõem componentes e chamam hooks
├── components/      ← reutilizáveis; cada um com seu .module.css quando precisar
├── hooks/           ← useWeather, useCities, useDebounce…
├── services/        ← client.js (instância axios) + um arquivo por recurso (auth.js, cities.js, weather.js)
├── theme/           ← tokens do AntD (claro/escuro)
└── assets/
```

## Rotas

| Rota | Página | Protegida |
|---|---|---|
| `/signin`, `/signup`, `/signout` | Auth | Não |
| `/p` | Home (busca + histórico) | Sim |
| `/p/weather/:cityName` | Detalhes do clima | Sim |
| `/p/favorites` | Favoritos | Sim |

Proteção de rota **só** em `ProtectedRoute`/router — páginas não redirecionam por estado de auth.

## Chamadas à API

- Toda chamada passa por `services/`; páginas e componentes nunca usam `axios` direto.
- URL base de `import.meta.env.VITE_API_URL` (**a partir da Fase 02**; hoje fixa — A27). `web/.env.example` versionado, `.env` local ignorado.
- **(a partir da Fase 02)** Instância única do axios com interceptor de request (`Authorization: Bearer <token>`) e de response (401 → limpa sessão e vai para `/signin`). `userId` nunca é enviado pelo front.
- Parâmetros de rota/query com `encodeURIComponent`.
- Erros da API chegam como `ProblemDetails`: exibir `title`/`detail`, não a mensagem do axios.

## Tema e estilo **(a partir da Fase 07)**

- Tokens do AntD em `theme/` aplicados via `ConfigProvider` (`token` + `algorithm` `defaultAlgorithm`/`darkAlgorithm`). Cores, fontes, raios e sombras **só** via token — nada de `#hex` solto em componentes.
- Preferência claro/escuro em `localStorage`, padrão = preferência do sistema.
- Layout responsivo com `Grid`/`Flex` do AntD e breakpoints do tema; sem larguras fixas em px para containers.
- Estilo de componente em `.module.css`; `style={{}}` inline só para valor dinâmico (ex.: imagem de fundo).
- Feedback: `message`/`notification`/`Modal.confirm` do AntD — nunca `alert()`/`confirm()`.
- Carregamento com `Skeleton` no formato do conteúdo; spinner só para ações curtas.

## Estado

- Estado de tela fica na página (ou em hook da página).
- Contexto global só para o que é realmente global: sessão (auth) e tema.

## Componentização

- JSX repetido em duas páginas → componente em `components/`.
- Header e footer vêm do layout, não de cada página.

## Lint

```bash
npm --prefix web run lint
npm --prefix web run build
```

## Ao concluir

- Padrão novo → esta página + linha nova no [`review-checklist.md`](review-checklist.md).
