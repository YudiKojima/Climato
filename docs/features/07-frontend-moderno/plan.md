# Fase 07a — Fundação visual: Plano de Implementação

> **Para agentes:** usar `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`) tarefa a tarefa. Passos com checkbox (`- [ ]`) — marcar ao concluir (regra GE-05).

**Objetivo:** reorganizar o `web/` na estrutura-alvo, aplicar o tema próprio Céu (claro/escuro persistido) com `AppLayout`/`AuthLayout`, redesenhar as cinco telas com CSS Modules, skeletons, estados vazios/erro e feedback do AntD, e introduzir Vitest para a lógica — conforme o [design aprovado](design.md).

**Arquitetura:** primeiro a infraestrutura de testes e a lógica pura (sessão, serviços, erros, tema, hooks), cada uma com teste antes do código; depois a casca da aplicação (providers, rotas, layouts, telas de auth); depois as telas internas; por fim documentação, CI e fechamento da 07a. Cada tarefa termina com lint, testes e build verdes e vira um commit.

**Stack:** React 19 · Vite 6 · Ant Design 5.29 (`cssVar`, `App.useApp`) · React Router 7 · axios · Vitest 3 · Testing Library · jsdom.

## Restrições globais

- Ler [`frontend.md`](../../architecture/frontend.md) antes de editar. O design vale mais que este plano em caso de conflito.
- **Nenhuma mudança em `src/` (backend).**
- TDD para toda lógica em `services/`, `hooks/`, `theme/` e `utils/`: teste primeiro, ver falhar, implementar, ver passar. Componentes visuais não têm teste (design §6).
- Cores, fontes, raios e sombras **só** em `web/src/theme/tokens.js`; nos `.module.css`, só `var(--ant-*)` (FE-06). `style={{}}` só com valor dinâmico (FE-07). Sem `width` fixo em container (FE-08).
- Feedback **só** via `App.useApp()` (`message`, `notification`, `modal`) — nunca os estáticos do `antd` nem `alert()`/`confirm()`.
- Textos de interface em português.
- Rodar o web com `npm --prefix web run dev` (porta 5173). **Não** subir o serviço `web` do compose: a 8080 está ocupada por outro projeto na máquina do usuário.
- **Commits:** um por tarefa (0–7), com as mensagens indicadas. A aprovação deste plano é a autorização explícita para esses commits (mesma exceção das fases anteriores à regra "nunca commitar durante a fase"). Antes de cada commit, `/revisar-padrao`; com 🔴, parar.
- Mensagens de commit em português, imperativo, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Branch: `dev/frontend-moderno` (já criada a partir do `origin/master`, sem upstream).
- Comando de verificação ao fim de cada tarefa (a partir da Tarefa 0):

```bash
npm --prefix web run lint
npm --prefix web test
npm --prefix web run build
```

Esperado: lint sem erros, todos os testes `passed`, build `✓ built`.

### Ajustes em relação ao design (decididos ao detalhar)

| Design | Plano | Motivo |
|---|---|---|
| `AuthProvider` expõe `isAuthenticated` | Expõe só `{ userName, signIn, signOut }`; `ProtectedRoute`/`GuestRoute` continuam chamando `hasValidSession()` | Um booleano no contexto não vê a expiração do token; `hasValidSession()` vê a cada navegação. Nada consome `isAuthenticated` (YAGNI) |
| Hook e provider no mesmo arquivo | `ThemeContext.js`/`AuthContext.js` (contexto + hook) separados de `ThemeProvider.jsx`/`AuthProvider.jsx` | `react-refresh/only-export-components` avisa quando um `.jsx` exporta componente e hook |
| Notificação de erro descrita nas telas | Hook `useApiFeedback()` (`hooks/`) + componente `CorrelationId` | Mesmo tratamento em `CityCard`, `SignIn`, `SignUp` (FE-09) |
| Estados de lista na Home e em Favoritos | `CityGrid` recebe `{ cities, isLoading, error, reload }` e decide skeleton/erro/vazio/grade | Mesmo bloco nas duas páginas (FE-09) |
| `index.css` | Removido; `main.jsx` importa `antd/dist/reset.css` | Reset oficial do AntD 5; o `index.css` atual tem fonte e cor literais e classes mortas |
| Logo por tema | `Climato sem fundo preto.png` (traço preto) no claro, `Climato sem fundo.png` (traço branco) no escuro e no painel do `AuthLayout` | Conferido nas imagens; resolve o risco do design sem asset novo |
| `jsdom` | `^26.1.0` | O `jsdom` 31 exige Node `^24.15`; a máquina local tem 24.13 |

---

### Tarefa 0: Vitest e `userName` na sessão

**Arquivos:**
- Modificar: `web/package.json`, `web/package-lock.json` (via npm), `web/vite.config.js`, `web/src/services/session.js`
- Criar: `web/src/test/setup.js`, `web/src/services/session.test.js`

**Interfaces:**
- Produz: script `npm test` (`vitest run`); `saveSession(auth)` grava também `auth.user.name`; `getUserName(): string | null`; `clearSession()` limpa os três valores.

- [x] **Passo 1: Instalar as dependências de teste**

```bash
npm --prefix web install -D vitest@^3.2.7 @testing-library/react@^16.3.3 @testing-library/dom@^10.4.2 @testing-library/jest-dom@^6.10.0 jsdom@^26.1.0
```

Esperado: `added N packages`, `package.json` com as cinco entradas em `devDependencies`.

- [x] **Passo 2: Script `test` no `web/package.json`**

Em `"scripts"`, depois de `"lint"`:

```json
    "test": "vitest run",
```

- [x] **Passo 3: Configurar o Vitest em `web/vite.config.js`**

```js
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.js",
  },
});
```

- [x] **Passo 4: Criar `web/src/test/setup.js`**

```js
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

window.matchMedia ??= (query) => ({
  matches: false,
  media: query,
  addEventListener() {},
  removeEventListener() {},
  addListener() {},
  removeListener() {},
});

afterEach(() => {
  cleanup();
  localStorage.clear();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});
```

- [x] **Passo 5: Escrever o teste `web/src/services/session.test.js`**

```js
import { describe, expect, it } from "vitest";
import {
  clearSession,
  getToken,
  getUserName,
  hasValidSession,
  saveSession,
} from "./session";

const inMinutes = (minutes) =>
  new Date(Date.now() + minutes * 60_000).toISOString();

const auth = (expiresAt) => ({
  token: "token-de-teste",
  expiresAt,
  user: { name: "Ana" },
});

describe("session", () => {
  it("saveSession guarda token e nome do usuário", () => {
    saveSession(auth(inMinutes(5)));

    expect(getToken()).toBe("token-de-teste");
    expect(getUserName()).toBe("Ana");
  });

  it("hasValidSession é verdadeiro com expiração futura", () => {
    saveSession(auth(inMinutes(5)));

    expect(hasValidSession()).toBe(true);
  });

  it("hasValidSession é falso com expiração passada", () => {
    saveSession(auth(inMinutes(-5)));

    expect(hasValidSession()).toBe(false);
  });

  it("hasValidSession é falso sem sessão", () => {
    expect(hasValidSession()).toBe(false);
  });

  it("clearSession remove token, expiração e nome", () => {
    saveSession(auth(inMinutes(5)));

    clearSession();

    expect(getToken()).toBeNull();
    expect(getUserName()).toBeNull();
    expect(hasValidSession()).toBe(false);
  });
});
```

- [x] **Passo 6: Rodar e ver falhar**

Run: `npm --prefix web test`
Esperado: FAIL — `getUserName is not a function` (ou export ausente).

- [x] **Passo 7: Implementar em `web/src/services/session.js`**

```js
const TOKEN_KEY = "token";
const EXPIRES_AT_KEY = "expiresAt";
const USER_NAME_KEY = "userName";

export const saveSession = (auth) => {
  localStorage.setItem(TOKEN_KEY, auth.token);
  localStorage.setItem(EXPIRES_AT_KEY, auth.expiresAt);
  localStorage.setItem(USER_NAME_KEY, auth.user.name);
};

export const getToken = () => localStorage.getItem(TOKEN_KEY);

export const getUserName = () => localStorage.getItem(USER_NAME_KEY);

export const clearSession = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRES_AT_KEY);
  localStorage.removeItem(USER_NAME_KEY);
};

export const hasValidSession = () => {
  const token = getToken();
  const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);

  return Boolean(token && expiresAt && new Date(expiresAt) > new Date());
};
```

- [x] **Passo 8: Rodar e ver passar**

Run: `npm --prefix web test`
Esperado: 5 testes `passed`.

- [x] **Passo 9: Verificação completa** (lint, test, build — ver Restrições globais).

- [x] **Passo 10: Commit**

```bash
git add web/package.json web/package-lock.json web/vite.config.js web/src/test/setup.js web/src/services/session.js web/src/services/session.test.js
git commit -m "Adiciona Vitest no web e guarda o nome do usuário na sessão"
```

---

### Tarefa 1: Serviços por recurso, `errors.js` e `formatTime`

**Arquivos:**
- Criar: `web/src/services/client.js`, `auth.js`, `cities.js`, `weather.js`, `errors.js`, `errors.test.js`; `web/src/utils/formatTime.js`, `formatTime.test.js`
- Modificar (só imports): `web/src/pages/Home.jsx`, `Favorites.jsx`, `WeatherDetails.jsx`, `SignIn.jsx`, `SignUp.jsx`, `web/src/components/CityCard.jsx`
- Remover: `web/src/services/api.js`, `web/src/components/formatDatetime.jsx`

**Interfaces:**
- Produz:
  - `client` (default export de `client.js`): instância axios com os interceptors atuais.
  - `auth.js`: `login(data)`, `register(data)` → `AuthDto` (`{ token, expiresAt, user: { name, ... } }`).
  - `cities.js`: `getAllCities()`, `getAllFavorites()`, `toggleFavorite(id)`, `deleteCity(id)`.
  - `weather.js`: `getWeatherByCity(cityName)`.
  - `errors.js`: `getApiErrorMessage(error, fallback?) → string`, `getCorrelationId(error) → string | undefined`, `isUnexpectedError(error) → boolean`.
  - `utils/formatTime.js`: `formatTime(isoWithOffset) → "HH:mm"`.

- [x] **Passo 1: Escrever `web/src/services/errors.test.js`**

```js
import { describe, expect, it } from "vitest";
import {
  getApiErrorMessage,
  getCorrelationId,
  isUnexpectedError,
} from "./errors";

const apiError = (status, data = {}, headers = {}) => ({
  response: { status, data, headers },
});

describe("getApiErrorMessage", () => {
  it("prefere o primeiro erro de validação", () => {
    const error = apiError(400, {
      title: "Dados inválidos.",
      errors: { Email: ["E-mail inválido."], Password: ["Senha curta."] },
    });

    expect(getApiErrorMessage(error)).toBe("E-mail inválido.");
  });

  it("usa o detail quando não há erro de validação", () => {
    const error = apiError(409, { title: "Conflito", detail: "Cidade favorita." });

    expect(getApiErrorMessage(error)).toBe("Cidade favorita.");
  });

  it("usa o title quando não há detail", () => {
    expect(getApiErrorMessage(apiError(503, { title: "Serviço indisponível." }))).toBe(
      "Serviço indisponível."
    );
  });

  it("usa o fallback quando não há resposta", () => {
    expect(getApiErrorMessage(new Error("Network Error"), "Sem conexão.")).toBe(
      "Sem conexão."
    );
  });

  it("usa o fallback padrão sem fallback informado", () => {
    expect(getApiErrorMessage(apiError(500))).toBe(
      "Não foi possível concluir a operação."
    );
  });
});

describe("getCorrelationId", () => {
  it("lê o header X-Correlation-ID", () => {
    const error = apiError(500, {}, { "x-correlation-id": "4bf92f3577b34da6" });

    expect(getCorrelationId(error)).toBe("4bf92f3577b34da6");
  });

  it("é undefined sem o header", () => {
    expect(getCorrelationId(apiError(500))).toBeUndefined();
  });

  it("é undefined sem resposta", () => {
    expect(getCorrelationId(new Error("Network Error"))).toBeUndefined();
  });
});

describe("isUnexpectedError", () => {
  it.each([
    ["erro de rede", new Error("Network Error"), true],
    ["500", apiError(500), true],
    ["503", apiError(503), true],
    ["404", apiError(404), false],
    ["400", apiError(400), false],
  ])("%s → %s", (_, error, expected) => {
    expect(isUnexpectedError(error)).toBe(expected);
  });
});
```

- [x] **Passo 2: Escrever `web/src/utils/formatTime.test.js`**

```js
import { describe, expect, it } from "vitest";
import { formatTime } from "./formatTime";

describe("formatTime", () => {
  it("devolve a hora local da cidade, sem converter o fuso", () => {
    expect(formatTime("2026-11-05T06:12:00+09:00")).toBe("06:12");
  });
});
```

- [x] **Passo 3: Rodar e ver falhar**

Run: `npm --prefix web test`
Esperado: FAIL — módulos `./errors` e `./formatTime` não encontrados.

- [x] **Passo 4: Criar `web/src/services/errors.js`**

```js
const DEFAULT_MESSAGE = "Não foi possível concluir a operação.";

export const getApiErrorMessage = (error, fallback = DEFAULT_MESSAGE) => {
  const data = error?.response?.data;
  const firstValidationError = data?.errors
    ? Object.values(data.errors).flat()[0]
    : undefined;

  return firstValidationError || data?.detail || data?.title || fallback;
};

export const getCorrelationId = (error) =>
  error?.response?.headers?.["x-correlation-id"];

export const isUnexpectedError = (error) =>
  !error?.response || error.response.status >= 500;
```

- [x] **Passo 5: Criar `web/src/utils/formatTime.js`**

```js
export const formatTime = (isoWithOffset) => isoWithOffset.slice(11, 16);
```

- [x] **Passo 6: Rodar e ver passar**

Run: `npm --prefix web test`
Esperado: todos `passed` (5 de sessão + 13 de erros + 1 de hora).

- [x] **Passo 7: Criar `web/src/services/client.js`** (conteúdo do topo do `api.js` atual)

```js
import axios from "axios";
import { clearSession, getToken } from "./session";

const client = axios.create({ baseURL: import.meta.env.VITE_API_URL });

client.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

client.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthRoute = error.config?.url?.startsWith("/auth/");

    if (error.response?.status === 401 && !isAuthRoute) {
      clearSession();
      window.location.assign("/signin");
    }

    return Promise.reject(error);
  }
);

export default client;
```

- [x] **Passo 8: Criar `web/src/services/auth.js`**

```js
import client from "./client";

export const login = async (data) => {
  const response = await client.post("/auth/login", data);
  return response.data;
};

export const register = async (data) => {
  const response = await client.post("/auth/register", data);
  return response.data;
};
```

- [x] **Passo 9: Criar `web/src/services/cities.js`**

```js
import client from "./client";

export const getAllCities = async () => {
  const response = await client.get("/cities");
  return response.data;
};

export const getAllFavorites = async () => {
  const response = await client.get("/cities/favorites");
  return response.data;
};

export const toggleFavorite = async (id) => {
  const response = await client.patch(`/cities/${id}/favorite`);
  return response.data;
};

export const deleteCity = async (id) => {
  await client.delete(`/cities/${id}`);
};
```

- [x] **Passo 10: Criar `web/src/services/weather.js`**

```js
import client from "./client";

export const getWeatherByCity = async (cityName) => {
  const response = await client.get(`/weathers/${encodeURIComponent(cityName)}`);
  return response.data;
};
```

- [x] **Passo 11: Trocar os imports nas telas atuais** (sem mudar mais nada; as telas são reescritas nas Tarefas 4–6)

| Arquivo | Import antigo | Import novo |
|---|---|---|
| `pages/Home.jsx` | `import { getAllCities } from "../services/api";` | `import { getAllCities } from "../services/cities";` |
| `pages/Favorites.jsx` | `import { getAllFavorites } from "../services/api";` | `import { getAllFavorites } from "../services/cities";` |
| `pages/WeatherDetails.jsx` | `import { getApiErrorMessage, getWeatherByCity } from "../services/api";` e `import formatDatetime from "../components/formatDatetime";` | `import { getWeatherByCity } from "../services/weather";` + `import { getApiErrorMessage } from "../services/errors";` + `import { formatTime as formatDatetime } from "../utils/formatTime";` |
| `pages/SignIn.jsx` | `import { login, getApiErrorMessage } from "../services/api";` | `import { login } from "../services/auth";` + `import { getApiErrorMessage } from "../services/errors";` |
| `pages/SignUp.jsx` | `import { register, getApiErrorMessage } from "../services/api";` | `import { register } from "../services/auth";` + `import { getApiErrorMessage } from "../services/errors";` |
| `components/CityCard.jsx` | `import { toggleFavorite, deleteCity, getApiErrorMessage } from "../services/api";` (multilinha) | `import { deleteCity, toggleFavorite } from "../services/cities";` + `import { getApiErrorMessage } from "../services/errors";` |

- [x] **Passo 12: Remover os arquivos antigos**

```bash
git rm web/src/services/api.js web/src/components/formatDatetime.jsx
```

Conferir que nada mais os importa: `grep -rn "services/api\|components/formatDatetime" web/src` → nenhum resultado.

- [x] **Passo 13: Verificação completa.**

- [x] **Passo 14: Commit**

```bash
git add web/src
git commit -m "Divide os serviços do web por recurso e extrai o tratamento de erro da API"
```

---

### Tarefa 2: Tema Céu com modo claro/escuro persistido

**Arquivos:**
- Criar: `web/src/theme/themeStorage.js`, `themeStorage.test.js`, `tokens.js`, `ThemeContext.js`, `ThemeProvider.jsx`, `ThemeProvider.test.jsx`; `web/src/components/ThemeToggle.jsx`

**Interfaces:**
- Produz:
  - `themeStorage.js`: `getInitialMode() → "light" | "dark"`, `saveMode(mode)`; chave `climato.theme`.
  - `tokens.js`: `getThemeConfig(mode) → ThemeConfig` do AntD (com `cssVar: true`).
  - `ThemeContext.js`: `ThemeContext`, `useThemeMode() → { mode, toggleMode }`.
  - `ThemeProvider` (default): envolve `children` em `ThemeContext.Provider` + `ConfigProvider` (tema + `pt_BR`).
  - `ThemeToggle` (default): botão sol/lua.

- [x] **Passo 1: Escrever `web/src/theme/themeStorage.test.js`**

```js
import { describe, expect, it, vi } from "vitest";
import { getInitialMode, saveMode } from "./themeStorage";

const systemPrefersDark = (isDark) =>
  vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({ matches: isDark }));

describe("getInitialMode", () => {
  it("sem preferência salva, segue o sistema escuro", () => {
    systemPrefersDark(true);

    expect(getInitialMode()).toBe("dark");
  });

  it("sem preferência salva, segue o sistema claro", () => {
    systemPrefersDark(false);

    expect(getInitialMode()).toBe("light");
  });

  it("preferência salva vence o sistema", () => {
    systemPrefersDark(true);
    saveMode("light");

    expect(getInitialMode()).toBe("light");
  });

  it("ignora valor salvo inválido", () => {
    systemPrefersDark(true);
    localStorage.setItem("climato.theme", "roxo");

    expect(getInitialMode()).toBe("dark");
  });

  it("localStorage bloqueado não quebra e segue o sistema", () => {
    systemPrefersDark(false);
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });

    expect(() => saveMode("dark")).not.toThrow();
    expect(getInitialMode()).toBe("light");
  });
});
```

- [x] **Passo 2: Escrever `web/src/theme/ThemeProvider.test.jsx`**

```jsx
import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ThemeProvider from "./ThemeProvider";
import { useThemeMode } from "./ThemeContext";

const wrapper = ({ children }) => <ThemeProvider>{children}</ThemeProvider>;

describe("ThemeProvider", () => {
  it("toggleMode alterna o modo e persiste a escolha", () => {
    vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({ matches: false }));
    const { result } = renderHook(() => useThemeMode(), { wrapper });

    expect(result.current.mode).toBe("light");

    act(() => result.current.toggleMode());

    expect(result.current.mode).toBe("dark");
    expect(localStorage.getItem("climato.theme")).toBe("dark");
  });

  it("não persiste nada antes de o usuário escolher", () => {
    renderHook(() => useThemeMode(), { wrapper });

    expect(localStorage.getItem("climato.theme")).toBeNull();
  });
});
```

- [x] **Passo 3: Rodar e ver falhar**

Run: `npm --prefix web test`
Esperado: FAIL — `./themeStorage`, `./ThemeProvider`, `./ThemeContext` não encontrados.

- [x] **Passo 4: Criar `web/src/theme/themeStorage.js`**

```js
const STORAGE_KEY = "climato.theme";
const MODES = ["light", "dark"];

const readSavedMode = () => {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
};

const systemPrefersDark = () =>
  window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;

export const getInitialMode = () => {
  const saved = readSavedMode();

  if (MODES.includes(saved)) {
    return saved;
  }

  return systemPrefersDark() ? "dark" : "light";
};

export const saveMode = (mode) => {
  try {
    localStorage.setItem(STORAGE_KEY, mode);
  } catch {
    // armazenamento bloqueado (ex.: modo privado): a escolha vale só nesta aba
  }
};
```

- [x] **Passo 5: Criar `web/src/theme/tokens.js`**

```js
import { theme } from "antd";

const baseToken = {
  colorPrimary: "#0ea5e9",
  colorInfo: "#0ea5e9",
  borderRadius: 12,
  fontFamily:
    "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
  boxShadow:
    "0 6px 16px 0 rgba(15, 23, 42, 0.08), 0 3px 6px -4px rgba(15, 23, 42, 0.12)",
};

const palettes = {
  light: {
    algorithm: theme.defaultAlgorithm,
    token: { colorBgLayout: "#f0f7fc" },
    headerBg: "#ffffff",
  },
  dark: {
    algorithm: theme.darkAlgorithm,
    token: {
      colorBgLayout: "#0b1220",
      colorBgContainer: "#111a2e",
      colorBgElevated: "#16213a",
    },
    headerBg: "#111a2e",
  },
};

export const getThemeConfig = (mode) => {
  const palette = palettes[mode];

  return {
    algorithm: palette.algorithm,
    token: { ...baseToken, ...palette.token },
    components: {
      Layout: {
        headerBg: palette.headerBg,
        headerHeight: 72,
        headerPadding: "0 24px",
        footerBg: "transparent",
      },
    },
    cssVar: true,
  };
};
```

- [x] **Passo 6: Criar `web/src/theme/ThemeContext.js`**

```js
import { createContext, useContext } from "react";

export const ThemeContext = createContext({
  mode: "light",
  toggleMode: () => {},
});

export const useThemeMode = () => useContext(ThemeContext);
```

- [x] **Passo 7: Criar `web/src/theme/ThemeProvider.jsx`**

```jsx
import { useCallback, useMemo, useState } from "react";
import { ConfigProvider } from "antd";
import ptBR from "antd/locale/pt_BR";
import { ThemeContext } from "./ThemeContext";
import { getInitialMode, saveMode } from "./themeStorage";
import { getThemeConfig } from "./tokens";

export default function ThemeProvider({ children }) {
  const [mode, setMode] = useState(getInitialMode);

  const toggleMode = useCallback(() => {
    const next = mode === "dark" ? "light" : "dark";
    saveMode(next);
    setMode(next);
  }, [mode]);

  const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode]);
  const themeConfig = useMemo(() => getThemeConfig(mode), [mode]);

  return (
    <ThemeContext.Provider value={value}>
      <ConfigProvider theme={themeConfig} locale={ptBR}>
        {children}
      </ConfigProvider>
    </ThemeContext.Provider>
  );
}
```

- [x] **Passo 8: Rodar e ver passar**

Run: `npm --prefix web test`
Esperado: todos `passed` (+7 desta tarefa).

- [x] **Passo 9: Criar `web/src/components/ThemeToggle.jsx`**

```jsx
import { Button, Tooltip } from "antd";
import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { useThemeMode } from "../theme/ThemeContext";

export default function ThemeToggle() {
  const { mode, toggleMode } = useThemeMode();
  const isDark = mode === "dark";
  const label = isDark ? "Usar tema claro" : "Usar tema escuro";

  return (
    <Tooltip title={label}>
      <Button
        type="text"
        shape="circle"
        aria-label={label}
        icon={isDark ? <SunOutlined /> : <MoonOutlined />}
        onClick={toggleMode}
      />
    </Tooltip>
  );
}
```

O `ThemeProvider` e o `ThemeToggle` só entram na árvore na Tarefa 4.

- [x] **Passo 10: Verificação completa.**

- [x] **Passo 11: Commit**

```bash
git add web/src/theme web/src/components/ThemeToggle.jsx
git commit -m "Adiciona o tema Céu do Ant Design com modo claro e escuro persistido"
```

---

### Tarefa 3: Hooks de carga e de feedback de erro

**Arquivos:**
- Criar: `web/src/hooks/useAsync.js`, `useAsync.test.js`, `useCities.js`, `useFavorites.js`, `useWeather.js`, `useApiFeedback.jsx`, `useApiFeedback.test.jsx`; `web/src/components/CorrelationId.jsx`

**Interfaces:**
- Consome: `getAllCities`, `getAllFavorites` (`services/cities.js`); `getWeatherByCity` (`services/weather.js`); `getApiErrorMessage`, `getCorrelationId`, `isUnexpectedError` (`services/errors.js`).
- Produz:
  - `useAsync(fn)` → `{ data, isLoading, error, reload }`. `fn` precisa ser estável (função de módulo ou `useCallback`); trocar `fn` refaz a carga e descarta a resposta da anterior. `reload()` refaz sem limpar `data`.
  - `useCities()`, `useFavorites()`, `useWeather(cityName)` → o mesmo objeto.
  - `useApiFeedback()` → `notifyError(error, fallback)`: erro de negócio → `message.error`; erro inesperado (rede/5xx) → `notification.error` com o `CorrelationId` quando houver.
  - `CorrelationId` (default): `({ id })` → “Código do erro: `<id>`” copiável.

- [x] **Passo 1: Escrever `web/src/hooks/useAsync.test.js`**

```js
import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import useAsync from "./useAsync";

describe("useAsync", () => {
  it("começa carregando e devolve os dados", async () => {
    const fn = vi.fn().mockResolvedValue(["Tóquio"]);
    const { result } = renderHook(() => useAsync(fn));

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.data).toEqual(["Tóquio"]);
    expect(result.current.error).toBeNull();
  });

  it("devolve o erro sem dados", async () => {
    const error = new Error("falhou");
    const fn = vi.fn().mockRejectedValue(error);
    const { result } = renderHook(() => useAsync(fn));

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.error).toBe(error);
    expect(result.current.data).toBeUndefined();
  });

  it("reload chama a função de novo e mantém os dados durante a carga", async () => {
    const fn = vi.fn().mockResolvedValueOnce("primeira").mockResolvedValueOnce("segunda");
    const { result } = renderHook(() => useAsync(fn));
    await waitFor(() => expect(result.current.data).toBe("primeira"));

    act(() => result.current.reload());

    expect(result.current.data).toBe("primeira");
    await waitFor(() => expect(result.current.data).toBe("segunda"));
    expect(fn).toHaveBeenCalledTimes(2);
  });

  it("descarta a resposta da função anterior", async () => {
    let resolveOld;
    const oldFn = vi.fn(() => new Promise((resolve) => { resolveOld = resolve; }));
    const newFn = vi.fn().mockResolvedValue("nova");
    const { result, rerender } = renderHook(({ fn }) => useAsync(fn), {
      initialProps: { fn: oldFn },
    });

    rerender({ fn: newFn });
    await waitFor(() => expect(result.current.data).toBe("nova"));
    await act(async () => resolveOld("antiga"));

    expect(result.current.data).toBe("nova");
  });
});
```

- [x] **Passo 2: Escrever `web/src/hooks/useApiFeedback.test.jsx`**

```jsx
import { act, renderHook, screen } from "@testing-library/react";
import { App } from "antd";
import { describe, expect, it } from "vitest";
import useApiFeedback from "./useApiFeedback";

const wrapper = ({ children }) => <App>{children}</App>;

const apiError = (status, data) => ({
  response: { status, data, headers: { "x-correlation-id": "4bf92f3577b34da6" } },
});

describe("useApiFeedback", () => {
  it("erro de negócio mostra a mensagem sem o código", async () => {
    const { result } = renderHook(() => useApiFeedback(), { wrapper });

    act(() => result.current(apiError(409, { detail: "Cidade favorita." })));

    expect(await screen.findByText("Cidade favorita.")).toBeInTheDocument();
    expect(screen.queryByText("4bf92f3577b34da6")).not.toBeInTheDocument();
  });

  it("erro 5xx mostra a mensagem e o código do erro", async () => {
    const { result } = renderHook(() => useApiFeedback(), { wrapper });

    act(() => result.current(apiError(503, { title: "Serviço indisponível." })));

    expect(await screen.findByText("Serviço indisponível.")).toBeInTheDocument();
    expect(await screen.findByText("4bf92f3577b34da6")).toBeInTheDocument();
  });

  it("erro de rede usa o fallback", async () => {
    const { result } = renderHook(() => useApiFeedback(), { wrapper });

    act(() => result.current(new Error("Network Error"), "Sem conexão com o servidor."));

    expect(await screen.findByText("Sem conexão com o servidor.")).toBeInTheDocument();
  });
});
```

- [x] **Passo 3: Rodar e ver falhar**

Run: `npm --prefix web test`
Esperado: FAIL — `./useAsync` e `./useApiFeedback` não encontrados.

- [x] **Passo 4: Criar `web/src/hooks/useAsync.js`**

```js
import { useCallback, useEffect, useState } from "react";

export default function useAsync(fn) {
  const [state, setState] = useState({
    data: undefined,
    isLoading: true,
    error: null,
  });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let isCurrent = true;
    setState((previous) => ({ ...previous, isLoading: true, error: null }));

    fn().then(
      (data) => {
        if (isCurrent) {
          setState({ data, isLoading: false, error: null });
        }
      },
      (error) => {
        if (isCurrent) {
          setState({ data: undefined, isLoading: false, error });
        }
      }
    );

    return () => {
      isCurrent = false;
    };
  }, [fn, attempt]);

  const reload = useCallback(() => setAttempt((count) => count + 1), []);

  return { ...state, reload };
}
```

- [x] **Passo 5: Criar `web/src/components/CorrelationId.jsx`**

```jsx
import { Typography } from "antd";

export default function CorrelationId({ id }) {
  return (
    <Typography.Text type="secondary">
      Código do erro:{" "}
      <Typography.Text code copyable>
        {id}
      </Typography.Text>
    </Typography.Text>
  );
}
```

- [x] **Passo 6: Criar `web/src/hooks/useApiFeedback.jsx`**

```jsx
import { useCallback } from "react";
import { App } from "antd";
import CorrelationId from "../components/CorrelationId";
import {
  getApiErrorMessage,
  getCorrelationId,
  isUnexpectedError,
} from "../services/errors";

export default function useApiFeedback() {
  const { message, notification } = App.useApp();

  return useCallback(
    (error, fallback) => {
      const text = getApiErrorMessage(error, fallback);

      if (!isUnexpectedError(error)) {
        message.error(text);
        return;
      }

      const correlationId = getCorrelationId(error);
      notification.error({
        message: text,
        description: correlationId && <CorrelationId id={correlationId} />,
      });
    },
    [message, notification]
  );
}
```

- [x] **Passo 7: Criar os hooks de recurso**

`web/src/hooks/useCities.js`:

```js
import { getAllCities } from "../services/cities";
import useAsync from "./useAsync";

export default function useCities() {
  return useAsync(getAllCities);
}
```

`web/src/hooks/useFavorites.js`:

```js
import { getAllFavorites } from "../services/cities";
import useAsync from "./useAsync";

export default function useFavorites() {
  return useAsync(getAllFavorites);
}
```

`web/src/hooks/useWeather.js`:

```js
import { useCallback } from "react";
import { getWeatherByCity } from "../services/weather";
import useAsync from "./useAsync";

export default function useWeather(cityName) {
  const load = useCallback(() => getWeatherByCity(cityName), [cityName]);
  return useAsync(load);
}
```

Os três só ligam um serviço ao `useAsync` (coberto acima); não ganham teste próprio.

- [x] **Passo 8: Rodar e ver passar**

Run: `npm --prefix web test`
Esperado: todos `passed` (+7 desta tarefa).

- [x] **Passo 9: Verificação completa.**

- [x] **Passo 10: Commit**

```bash
git add web/src/hooks web/src/components/CorrelationId.jsx
git commit -m "Adiciona hooks de carga e de feedback de erro com o correlation ID"
```

---

### Tarefa 4: Providers, rotas, layouts e telas de login/cadastro

**Arquivos:**
- Criar: `web/src/app/AppProviders.jsx`, `AuthContext.js`, `AuthProvider.jsx`, `router.jsx`, `ProtectedRoute.jsx`, `GuestRoute.jsx`; `web/src/layouts/AppLayout.jsx`, `AppLayout.module.css`, `AuthLayout.jsx`, `AuthLayout.module.css`; `web/src/components/UserMenu.jsx`, `UserMenu.module.css`; `web/src/pages/Auth.module.css`
- Modificar: `web/src/main.jsx`, `web/src/pages/SignIn.jsx`, `web/src/pages/SignUp.jsx`
- Remover: `web/src/App.jsx`, `web/src/App.css`, `web/src/index.css`, `web/src/router/` (pasta), `web/src/pages/SignOut.jsx`, `web/src/styles/Containers.css`

**Interfaces:**
- Consome: `ThemeProvider`, `useThemeMode`, `ThemeToggle` (Tarefa 2); `useApiFeedback` (Tarefa 3); `login`, `register` (Tarefa 1); `saveSession`, `clearSession`, `getUserName`, `hasValidSession` (Tarefa 0).
- Produz:
  - `AuthContext.js`: `useAuth() → { userName: string, signIn(auth), signOut() }`.
  - `AppLayout` e `AuthLayout` renderizam `<Outlet/>`.
  - Rotas: `/signin`, `/signup` (guest + `AuthLayout`); `/p`, `/p/weather/:cityName`, `/p/favorites` (protegidas + `AppLayout`).

- [x] **Passo 1: Criar `web/src/app/AuthContext.js`**

```js
import { createContext, useContext } from "react";

export const AuthContext = createContext(null);

export const useAuth = () => useContext(AuthContext);
```

- [x] **Passo 2: Criar `web/src/app/AuthProvider.jsx`**

```jsx
import { useCallback, useMemo, useState } from "react";
import { clearSession, getUserName, saveSession } from "../services/session";
import { AuthContext } from "./AuthContext";

const FALLBACK_NAME = "Usuário";

export default function AuthProvider({ children }) {
  const [userName, setUserName] = useState(() => getUserName() ?? FALLBACK_NAME);

  const signIn = useCallback((auth) => {
    saveSession(auth);
    setUserName(auth.user.name);
  }, []);

  const signOut = useCallback(() => {
    clearSession();
    setUserName(FALLBACK_NAME);
  }, []);

  const value = useMemo(
    () => ({ userName, signIn, signOut }),
    [userName, signIn, signOut]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
```

`FALLBACK_NAME` cobre sessões gravadas antes desta fase, que não têm `userName`.

- [x] **Passo 3: Criar `web/src/app/AppProviders.jsx`**

```jsx
import { App } from "antd";
import ThemeProvider from "../theme/ThemeProvider";
import AuthProvider from "./AuthProvider";

export default function AppProviders({ children }) {
  return (
    <ThemeProvider>
      <App>
        <AuthProvider>{children}</AuthProvider>
      </App>
    </ThemeProvider>
  );
}
```

- [x] **Passo 4: Criar os guards**

`web/src/app/ProtectedRoute.jsx`:

```jsx
import { Navigate, Outlet } from "react-router-dom";
import { hasValidSession } from "../services/session";

export default function ProtectedRoute() {
  return hasValidSession() ? <Outlet /> : <Navigate to="/signin" replace />;
}
```

`web/src/app/GuestRoute.jsx`:

```jsx
import { Navigate, Outlet } from "react-router-dom";
import { hasValidSession } from "../services/session";

export default function GuestRoute() {
  return hasValidSession() ? <Navigate to="/p" replace /> : <Outlet />;
}
```

- [x] **Passo 5: Criar `web/src/components/UserMenu.jsx` e `UserMenu.module.css`**

```jsx
import { Avatar, Button, Dropdown } from "antd";
import { HomeOutlined, LogoutOutlined, StarOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../app/AuthContext";
import styles from "./UserMenu.module.css";

const navigationItems = [
  { key: "/p", label: "Início", icon: <HomeOutlined /> },
  { key: "/p/favorites", label: "Favoritos", icon: <StarOutlined /> },
  { type: "divider" },
];

export default function UserMenu({ showName, showNavigation }) {
  const { userName, signOut } = useAuth();
  const navigate = useNavigate();

  const items = [
    ...(showNavigation ? navigationItems : []),
    { key: "signout", label: "Sair", icon: <LogoutOutlined />, danger: true },
  ];

  const handleClick = ({ key }) => {
    if (key === "signout") {
      signOut();
      navigate("/signin", { replace: true });
      return;
    }

    navigate(key);
  };

  return (
    <Dropdown
      menu={{ items, onClick: handleClick }}
      trigger={["click"]}
      placement="bottomRight"
    >
      <Button type="text" className={styles.trigger} aria-label="Menu do usuário">
        <Avatar size="small" className={styles.avatar}>
          {userName.charAt(0).toUpperCase()}
        </Avatar>
        {showName && <span>{userName}</span>}
      </Button>
    </Dropdown>
  );
}
```

```css
.trigger {
  display: flex;
  align-items: center;
  gap: var(--ant-margin-xs);
}

.avatar {
  background: var(--ant-color-primary);
}
```

- [x] **Passo 6: Criar `web/src/layouts/AppLayout.jsx` e `AppLayout.module.css`**

```jsx
import { Grid, Layout } from "antd";
import { Link, NavLink, Outlet } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import UserMenu from "../components/UserMenu";
import { useThemeMode } from "../theme/ThemeContext";
import logoLight from "../assets/Climato sem fundo preto.png";
import logoDark from "../assets/Climato sem fundo.png";
import styles from "./AppLayout.module.css";

const navLinkClass = ({ isActive }) =>
  isActive ? `${styles.navLink} ${styles.active}` : styles.navLink;

export default function AppLayout() {
  const screens = Grid.useBreakpoint();
  const { mode } = useThemeMode();
  const isWide = Boolean(screens.md);

  return (
    <Layout className={styles.root}>
      <Layout.Header className={styles.header}>
        <Link to="/p" className={styles.brand}>
          <img
            src={mode === "dark" ? logoDark : logoLight}
            alt="Climato"
            className={styles.logo}
          />
        </Link>
        <nav className={styles.actions}>
          {isWide && (
            <>
              <NavLink to="/p" end className={navLinkClass}>
                Início
              </NavLink>
              <NavLink to="/p/favorites" className={navLinkClass}>
                Favoritos
              </NavLink>
            </>
          )}
          <ThemeToggle />
          <UserMenu showName={isWide} showNavigation={!isWide} />
        </nav>
      </Layout.Header>
      <Layout.Content className={styles.content}>
        <div className={styles.container}>
          <Outlet />
        </div>
      </Layout.Content>
      <Layout.Footer className={styles.footer}>
        Climato © {new Date().getFullYear()}
      </Layout.Footer>
    </Layout>
  );
}
```

```css
.root {
  min-height: 100vh;
}

.header {
  position: sticky;
  top: 0;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--ant-margin);
  border-bottom: 1px solid var(--ant-color-border-secondary);
}

.brand {
  display: flex;
  align-items: center;
}

.logo {
  height: 56px;
}

.actions {
  display: flex;
  align-items: center;
  gap: var(--ant-margin-sm);
  line-height: normal;
}

.navLink {
  color: var(--ant-color-text-secondary);
  font-weight: 500;
  padding: var(--ant-padding-xxs) var(--ant-padding-sm);
  border-radius: var(--ant-border-radius);
  transition: color 0.2s ease, background-color 0.2s ease;
}

.navLink:hover {
  color: var(--ant-color-primary);
}

.active {
  color: var(--ant-color-primary);
  background: var(--ant-color-primary-bg);
}

.content {
  padding: var(--ant-padding-lg) var(--ant-padding);
}

.container {
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
}

.footer {
  text-align: center;
  color: var(--ant-color-text-tertiary);
}
```

- [x] **Passo 7: Criar `web/src/layouts/AuthLayout.jsx` e `AuthLayout.module.css`**

```jsx
import { Col, Layout, Row, Typography } from "antd";
import { Outlet } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import { useThemeMode } from "../theme/ThemeContext";
import logoLight from "../assets/Climato sem fundo preto.png";
import logoDark from "../assets/Climato sem fundo.png";
import styles from "./AuthLayout.module.css";

export default function AuthLayout() {
  const { mode } = useThemeMode();

  return (
    <Layout className={styles.root}>
      <Row className={styles.row}>
        <Col xs={0} md={12} lg={14} className={styles.aside}>
          <div className={styles.overlay}>
            <img src={logoDark} alt="" className={styles.asideLogo} />
            <Typography.Title level={2} className={styles.tagline}>
              O clima de qualquer cidade, com a cara dela.
            </Typography.Title>
          </div>
        </Col>
        <Col xs={24} md={12} lg={10} className={styles.main}>
          <div className={styles.toggle}>
            <ThemeToggle />
          </div>
          <div className={styles.form}>
            <img
              src={mode === "dark" ? logoDark : logoLight}
              alt="Climato"
              className={styles.logo}
            />
            <Outlet />
          </div>
        </Col>
      </Row>
    </Layout>
  );
}
```

```css
.root,
.row {
  min-height: 100vh;
}

.aside {
  position: relative;
  background: url("../assets/background.png") center / cover no-repeat;
}

.overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: var(--ant-margin);
  padding: var(--ant-padding-xl);
  background: linear-gradient(to top, var(--ant-color-bg-mask), transparent 70%);
}

.asideLogo {
  width: 10rem;
}

.overlay .tagline {
  color: var(--ant-color-white);
  margin: 0;
}

.main {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--ant-padding-xl) var(--ant-padding-lg);
  background: var(--ant-color-bg-container);
}

.toggle {
  position: absolute;
  top: var(--ant-padding);
  right: var(--ant-padding);
}

.form {
  width: 100%;
  max-width: 24rem;
  display: flex;
  flex-direction: column;
  gap: var(--ant-margin);
}

.logo {
  width: 8rem;
  align-self: center;
}
```

- [x] **Passo 8: Criar `web/src/pages/Auth.module.css`**

```css
.switch {
  display: block;
  text-align: center;
}
```

- [x] **Passo 9: Reescrever `web/src/pages/SignIn.jsx`**

```jsx
import { useState } from "react";
import { Button, Form, Input, Typography } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../app/AuthContext";
import useApiFeedback from "../hooks/useApiFeedback";
import { login } from "../services/auth";
import styles from "./Auth.module.css";

export default function SignIn() {
  const navigate = useNavigate();
  const { signIn } = useAuth();
  const notifyError = useApiFeedback();
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (values) => {
    setIsLoading(true);
    try {
      signIn(await login(values));
      navigate("/p", { replace: true });
    } catch (err) {
      notifyError(err, "E-mail ou senha incorretos.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <div>
        <Typography.Title level={3}>Acessar conta</Typography.Title>
        <Typography.Text type="secondary">
          Entre para ver o clima das suas cidades.
        </Typography.Text>
      </div>

      <Form
        layout="vertical"
        validateTrigger="onBlur"
        requiredMark={false}
        size="large"
        onFinish={handleSubmit}
      >
        <Form.Item
          label="E-mail"
          name="email"
          rules={[
            { required: true, message: "Digite seu e-mail" },
            { type: "email", message: "Digite um e-mail válido" },
          ]}
        >
          <Input autoComplete="email" placeholder="voce@exemplo.com" />
        </Form.Item>

        <Form.Item
          label="Senha"
          name="password"
          rules={[{ required: true, message: "Digite sua senha" }]}
        >
          <Input.Password autoComplete="current-password" placeholder="Digite sua senha" />
        </Form.Item>

        <Button type="primary" htmlType="submit" block loading={isLoading}>
          Entrar
        </Button>
      </Form>

      <Typography.Text className={styles.switch}>
        Não tem uma conta? <Link to="/signup">Criar conta</Link>
      </Typography.Text>
    </>
  );
}
```

- [x] **Passo 10: Reescrever `web/src/pages/SignUp.jsx`**

```jsx
import { useState } from "react";
import { Button, Form, Input, Typography } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../app/AuthContext";
import useApiFeedback from "../hooks/useApiFeedback";
import { register } from "../services/auth";
import styles from "./Auth.module.css";

export default function SignUp() {
  const navigate = useNavigate();
  const { signIn } = useAuth();
  const notifyError = useApiFeedback();
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (values) => {
    setIsLoading(true);
    try {
      signIn(await register(values));
      navigate("/p", { replace: true });
    } catch (err) {
      notifyError(err, "Erro ao criar conta.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <div>
        <Typography.Title level={3}>Criar conta</Typography.Title>
        <Typography.Text type="secondary">
          Salve suas cidades e acompanhe o clima de cada uma.
        </Typography.Text>
      </div>

      <Form
        layout="vertical"
        validateTrigger="onBlur"
        requiredMark={false}
        size="large"
        onFinish={handleSubmit}
      >
        <Form.Item
          label="Nome"
          name="name"
          rules={[{ required: true, message: "Digite seu nome" }]}
        >
          <Input autoComplete="name" placeholder="Digite seu nome" />
        </Form.Item>

        <Form.Item
          label="E-mail"
          name="email"
          rules={[
            { required: true, message: "Digite seu e-mail" },
            { type: "email", message: "E-mail inválido" },
          ]}
        >
          <Input autoComplete="email" placeholder="voce@exemplo.com" />
        </Form.Item>

        <Form.Item
          label="Senha"
          name="password"
          rules={[
            { required: true, message: "Digite sua senha" },
            { min: 8, message: "A senha deve ter pelo menos 8 caracteres" },
          ]}
        >
          <Input.Password autoComplete="new-password" placeholder="Mínimo de 8 caracteres" />
        </Form.Item>

        <Button type="primary" htmlType="submit" block loading={isLoading}>
          Criar conta
        </Button>
      </Form>

      <Typography.Text className={styles.switch}>
        Já tem uma conta? <Link to="/signin">Acessar conta</Link>
      </Typography.Text>
    </>
  );
}
```

- [x] **Passo 11: Criar `web/src/app/router.jsx`**

```jsx
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "../layouts/AppLayout";
import AuthLayout from "../layouts/AuthLayout";
import Favorites from "../pages/Favorites";
import Home from "../pages/Home";
import SignIn from "../pages/SignIn";
import SignUp from "../pages/SignUp";
import WeatherDetails from "../pages/WeatherDetails";
import GuestRoute from "./GuestRoute";
import ProtectedRoute from "./ProtectedRoute";

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<GuestRoute />}>
          <Route element={<AuthLayout />}>
            <Route path="/signin" element={<SignIn />} />
            <Route path="/signup" element={<SignUp />} />
          </Route>
        </Route>

        <Route path="/p" element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route index element={<Home />} />
            <Route path="weather/:cityName" element={<WeatherDetails />} />
            <Route path="favorites" element={<Favorites />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/signin" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
```

- [x] **Passo 12: Reescrever `web/src/main.jsx`**

```jsx
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "antd/dist/reset.css";
import AppProviders from "./app/AppProviders";
import AppRouter from "./app/router";

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <AppProviders>
      <AppRouter />
    </AppProviders>
  </StrictMode>
);
```

- [x] **Passo 13: Remover o que saiu**

```bash
git rm -r web/src/App.jsx web/src/App.css web/src/index.css web/src/router web/src/pages/SignOut.jsx web/src/styles/Containers.css
```

Conferir: `grep -rn "signout\|SignOut\|Containers.css\|index.css\|/router\"" web/src` → nenhum resultado. (Home e Favorites ainda têm header e footer próprios até a Tarefa 5 — duplicados dentro do `AppLayout` por um commit, esperado.)

- [x] **Passo 14: Verificação completa.**

- [x] **Passo 15: Conferência manual** (`npm --prefix web run dev`, backend local conforme o [`runbook.md`](../../runbook.md))
  - `/signin` em ≥ 768 px: duas colunas (imagem + formulário); em 375 px (DevTools): só o formulário com a logo.
  - Alternar o tema no `/signin`, recarregar: o tema se mantém. Apagar `climato.theme` no Application → Local Storage: volta a seguir o sistema.
  - DevTools → Elements: o elemento `.ant-app` tem classe `css-var-*` e `--ant-color-primary: #0ea5e9` em Computed. Se as `var(--ant-*)` não resolverem dentro dos layouts, parar e investigar com `superpowers:systematic-debugging` antes de seguir.
  - Login com senha errada: `message` vermelho com o texto da API. Login certo → `/p` com header (logo, Início, Favoritos, tema, avatar com a inicial e o nome).
  - Menu do avatar → Sair → `/signin`; voltar no navegador para `/p` → redireciona para `/signin`.
  - Com sessão válida, abrir `/signin` → vai para `/p`.

- [x] **Passo 16: Commit**

```bash
git add web/src
git commit -m "Adiciona providers de tema e sessão, rotas aninhadas, AppLayout e login/cadastro em tela dividida"
```

---

### Tarefa 5: Cards de cidade, estados de lista, Home e Favoritos

**Arquivos:**
- Criar: `web/src/components/EmptyState.jsx`, `EmptyState.module.css`, `ErrorState.jsx`, `CityGrid.jsx`, `CityGridSkeleton.jsx`, `CityCard.module.css`; `web/src/pages/Home.module.css`
- Modificar (reescrever): `web/src/components/CityCard.jsx`, `web/src/pages/Home.jsx`, `web/src/pages/Favorites.jsx`
- Remover: `web/src/components/Footer.jsx`, `web/src/styles/Favorites.css`

**Interfaces:**
- Consome: `useCities`, `useFavorites`, `useApiFeedback` (Tarefa 3); `CorrelationId` (Tarefa 3); `toggleFavorite`, `deleteCity` (Tarefa 1); `getApiErrorMessage`, `getCorrelationId`, `isUnexpectedError` (Tarefa 1).
- Produz:
  - `EmptyState({ description, children })`.
  - `ErrorState({ error, fallback, onRetry })` — usado também na Tarefa 6.
  - `CityCard({ city, onChange, onOpen })`; `CityGrid({ cities, isLoading, error, reload, emptyDescription, onOpen })`.

- [x] **Passo 1: Criar `web/src/components/EmptyState.jsx` e `EmptyState.module.css`**

```jsx
import { Empty } from "antd";
import styles from "./EmptyState.module.css";

export default function EmptyState({ description, children }) {
  return (
    <Empty
      className={styles.root}
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={description}
    >
      {children}
    </Empty>
  );
}
```

```css
.root {
  padding: var(--ant-padding-xl) 0;
}
```

- [x] **Passo 2: Criar `web/src/components/ErrorState.jsx`**

```jsx
import { Button, Result } from "antd";
import {
  getApiErrorMessage,
  getCorrelationId,
  isUnexpectedError,
} from "../services/errors";
import CorrelationId from "./CorrelationId";

export default function ErrorState({ error, fallback, onRetry }) {
  const correlationId = isUnexpectedError(error) ? getCorrelationId(error) : undefined;

  return (
    <Result
      status="warning"
      title={getApiErrorMessage(error, fallback)}
      subTitle={correlationId && <CorrelationId id={correlationId} />}
      extra={
        onRetry && (
          <Button type="primary" onClick={onRetry}>
            Tentar de novo
          </Button>
        )
      }
    />
  );
}
```

- [x] **Passo 3: Reescrever `web/src/components/CityCard.jsx` e criar `CityCard.module.css`** (resolve a pendência do `handleFavoriteClick` sem `try/catch`)

```jsx
import { useState } from "react";
import { App, Button, Card, Tooltip, Typography } from "antd";
import { DeleteOutlined, StarFilled, StarOutlined } from "@ant-design/icons";
import useApiFeedback from "../hooks/useApiFeedback";
import { deleteCity, toggleFavorite } from "../services/cities";
import styles from "./CityCard.module.css";

export default function CityCard({ city, onChange, onOpen }) {
  const { message, modal } = App.useApp();
  const notifyError = useApiFeedback();
  const [isSaving, setIsSaving] = useState(false);

  const handleFavorite = async (e) => {
    e.stopPropagation();
    setIsSaving(true);
    try {
      await toggleFavorite(city.id);
      message.success(
        city.isFavorite ? "Removida dos favoritos." : "Adicionada aos favoritos."
      );
      onChange();
    } catch (err) {
      notifyError(err, "Não foi possível atualizar a favorita.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = (e) => {
    e.stopPropagation();
    modal.confirm({
      title: `Remover ${city.name} do histórico?`,
      okText: "Remover",
      okButtonProps: { danger: true },
      cancelText: "Cancelar",
      onOk: async () => {
        try {
          await deleteCity(city.id);
          onChange();
        } catch (err) {
          notifyError(err, "Não foi possível remover a cidade.");
        }
      },
    });
  };

  return (
    <Card hoverable className={styles.card} onClick={() => onOpen(city)}>
      <div className={styles.header}>
        <div>
          <Typography.Title level={4} className={styles.title}>
            {city.name}
          </Typography.Title>
          <Typography.Text type="secondary">{city.country}</Typography.Text>
        </div>
        {!city.isFavorite && (
          <Tooltip title="Remover do histórico">
            <Button
              type="text"
              size="small"
              aria-label={`Remover ${city.name} do histórico`}
              icon={<DeleteOutlined />}
              onClick={handleDelete}
            />
          </Tooltip>
        )}
      </div>
      <Button
        className={styles.favorite}
        size="small"
        type={city.isFavorite ? "default" : "primary"}
        icon={city.isFavorite ? <StarFilled /> : <StarOutlined />}
        loading={isSaving}
        onClick={handleFavorite}
      >
        {city.isFavorite ? "Favorita" : "Favoritar"}
      </Button>
    </Card>
  );
}
```

```css
.card {
  height: 100%;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: var(--ant-box-shadow);
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--ant-margin-xs);
}

.card .title {
  margin: 0;
}

.favorite {
  margin-top: var(--ant-margin);
}

@media (prefers-reduced-motion: reduce) {
  .card,
  .card:hover {
    transition: none;
    transform: none;
  }
}
```

A favorita não mostra o botão remover (o backend recusa com 409 — regra A18); o aviso `message.warning` antigo some junto.

- [x] **Passo 4: Criar `web/src/components/CityGridSkeleton.jsx`**

```jsx
import { Card, Col, Row, Skeleton } from "antd";

export default function CityGridSkeleton({ count = 4 }) {
  return (
    <Row gutter={[16, 16]}>
      {Array.from({ length: count }, (_, index) => (
        <Col key={index} xs={24} sm={12} md={8} lg={6}>
          <Card>
            <Skeleton active paragraph={{ rows: 2 }} />
          </Card>
        </Col>
      ))}
    </Row>
  );
}
```

- [x] **Passo 5: Criar `web/src/components/CityGrid.jsx`**

```jsx
import { Col, Row } from "antd";
import CityCard from "./CityCard";
import CityGridSkeleton from "./CityGridSkeleton";
import EmptyState from "./EmptyState";
import ErrorState from "./ErrorState";

export default function CityGrid({
  cities,
  isLoading,
  error,
  reload,
  emptyDescription,
  onOpen,
}) {
  if (error) {
    return (
      <ErrorState
        error={error}
        fallback="Não foi possível carregar as cidades."
        onRetry={reload}
      />
    );
  }

  if (isLoading && !cities) {
    return <CityGridSkeleton />;
  }

  if (cities.length === 0) {
    return <EmptyState description={emptyDescription} />;
  }

  return (
    <Row gutter={[16, 16]}>
      {cities.map((city) => (
        <Col key={city.id} xs={24} sm={12} md={8} lg={6}>
          <CityCard city={city} onChange={reload} onOpen={onOpen} />
        </Col>
      ))}
    </Row>
  );
}
```

- [x] **Passo 6: Reescrever `web/src/pages/Home.jsx` e criar `Home.module.css`**

```jsx
import { Input, Typography } from "antd";
import { useNavigate } from "react-router-dom";
import CityGrid from "../components/CityGrid";
import useCities from "../hooks/useCities";
import styles from "./Home.module.css";

export default function Home() {
  const navigate = useNavigate();
  const { data, isLoading, error, reload } = useCities();

  const openCity = (name) => navigate(`/p/weather/${encodeURIComponent(name)}`);

  const handleSearch = (value) => {
    const name = value.trim();

    if (name) {
      openCity(name);
    }
  };

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <Typography.Title level={2}>Como está o tempo hoje?</Typography.Title>
        <Typography.Paragraph type="secondary">
          Pesquise uma cidade para ver o clima agora.
        </Typography.Paragraph>
        <Input.Search
          className={styles.search}
          placeholder="Buscar cidade..."
          enterButton="Buscar"
          size="large"
          allowClear
          onSearch={handleSearch}
        />
      </section>

      <section>
        <Typography.Title level={4}>Cidades pesquisadas</Typography.Title>
        <CityGrid
          cities={data}
          isLoading={isLoading}
          error={error}
          reload={reload}
          emptyDescription="Pesquise uma cidade para começar."
          onOpen={(city) => openCity(city.name)}
        />
      </section>
    </div>
  );
}
```

```css
.page {
  display: flex;
  flex-direction: column;
  gap: var(--ant-margin-xl);
}

.hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: var(--ant-padding-lg) 0;
}

.search {
  width: 100%;
  max-width: 40rem;
}
```

- [x] **Passo 7: Reescrever `web/src/pages/Favorites.jsx`**

```jsx
import { Typography } from "antd";
import { useNavigate } from "react-router-dom";
import CityGrid from "../components/CityGrid";
import useFavorites from "../hooks/useFavorites";

export default function Favorites() {
  const navigate = useNavigate();
  const { data, isLoading, error, reload } = useFavorites();

  return (
    <section>
      <Typography.Title level={2}>Cidades favoritas</Typography.Title>
      <CityGrid
        cities={data}
        isLoading={isLoading}
        error={error}
        reload={reload}
        emptyDescription="Nenhuma cidade favoritada ainda."
        onOpen={(city) => navigate(`/p/weather/${encodeURIComponent(city.name)}`)}
      />
    </section>
  );
}
```

- [x] **Passo 8: Remover o que saiu**

```bash
git rm web/src/components/Footer.jsx web/src/styles/Favorites.css
```

Conferir: `grep -rn "Footer\"\|Favorites.css\|clearSession" web/src/pages web/src/components` → nenhum resultado.

- [x] **Passo 9: Verificação completa.**

- [x] **Passo 10: Conferência manual**
  - Home: skeleton de cards ao carregar; usuário novo vê o estado vazio; busca com Enter abre o detalhe.
  - Favoritar/desfavoritar: botão em loading, `message` de sucesso, lista atualiza sem piscar skeleton.
  - Remover: `modal` de confirmação; Cancelar não remove; Remover some com o card.
  - Gateway parado: Home mostra `ErrorState` com “Tentar de novo” (sem código — erro de rede não tem header); subir o Gateway e clicar recarrega.
  - Favoritos: estado vazio quando não há favoritas; cards sem botão remover.
  - 375 px: um card por linha; 1440 px: quatro por linha, conteúdo centralizado.
  - Tema escuro: cards, header e textos legíveis.

- [x] **Passo 11: Commit**

```bash
git add web/src
git commit -m "Redesenha a Home e os Favoritos com skeleton, estados vazio e de erro e trata erro do favoritar"
```

---

### Tarefa 6: Detalhe do clima

**Arquivos:**
- Modificar (reescrever): `web/src/pages/WeatherDetails.jsx`
- Criar: `web/src/pages/WeatherDetails.module.css`
- Remover: `web/src/components/Loading.jsx`, `web/src/styles/` (pasta, sobra `WeatherDetails.css`)

**Interfaces:**
- Consome: `useWeather` (Tarefa 3); `EmptyState`, `ErrorState` (Tarefa 5); `formatTime` (Tarefa 1).
- Campos do `WeatherDto` usados: `city`, `country`, `description`, `icon`, `temperature`, `tempMin`, `tempMax`, `feelsLike`, `humidity`, `pressure`, `windSpeedKmh`, `windDirection`, `sunrise`, `sunset`, `imageUrl` (opcional).

- [x] **Passo 1: Reescrever `web/src/pages/WeatherDetails.jsx`**

```jsx
import { Button, Card, Col, Row, Skeleton, Space, Statistic, Typography } from "antd";
import {
  ArrowDownOutlined,
  ArrowLeftOutlined,
  ArrowUpOutlined,
  CloudOutlined,
  CompassOutlined,
  DashboardOutlined,
  FireOutlined,
  MoonOutlined,
  SendOutlined,
  SunOutlined,
} from "@ant-design/icons";
import { useNavigate, useParams } from "react-router-dom";
import EmptyState from "../components/EmptyState";
import ErrorState from "../components/ErrorState";
import useWeather from "../hooks/useWeather";
import { formatTime } from "../utils/formatTime";
import styles from "./WeatherDetails.module.css";

const METRIC_COLS = { xs: 12, md: 8 };

const toMetrics = (weather) => [
  { label: "Mínima", value: `${weather.tempMin}°C`, icon: <ArrowDownOutlined /> },
  { label: "Máxima", value: `${weather.tempMax}°C`, icon: <ArrowUpOutlined /> },
  { label: "Sensação", value: `${weather.feelsLike}°C`, icon: <FireOutlined /> },
  { label: "Umidade", value: `${weather.humidity}%`, icon: <CloudOutlined /> },
  { label: "Pressão", value: `${weather.pressure} hPa`, icon: <DashboardOutlined /> },
  { label: "Vento", value: `${weather.windSpeedKmh} km/h`, icon: <SendOutlined /> },
  { label: "Direção do vento", value: `${weather.windDirection}°`, icon: <CompassOutlined /> },
  { label: "Nascer do sol", value: formatTime(weather.sunrise), icon: <SunOutlined /> },
  { label: "Pôr do sol", value: formatTime(weather.sunset), icon: <MoonOutlined /> },
];

function WeatherDetailsSkeleton() {
  return (
    <div className={styles.page}>
      <Card className={styles.heroSkeleton}>
        <Skeleton active paragraph={{ rows: 4 }} />
      </Card>
      <Row gutter={[16, 16]}>
        {Array.from({ length: 6 }, (_, index) => (
          <Col key={index} {...METRIC_COLS}>
            <Card size="small">
              <Skeleton active title={false} paragraph={{ rows: 2 }} />
            </Card>
          </Col>
        ))}
      </Row>
    </div>
  );
}

export default function WeatherDetails() {
  const { cityName } = useParams();
  const navigate = useNavigate();
  const { data: weather, isLoading, error, reload } = useWeather(cityName);

  const backButton = (
    <Button icon={<ArrowLeftOutlined />} onClick={() => navigate("/p")}>
      Voltar
    </Button>
  );

  if (isLoading) {
    return <WeatherDetailsSkeleton />;
  }

  if (error?.response?.status === 404) {
    return (
      <EmptyState description={`Cidade "${cityName}" não encontrada.`}>
        {backButton}
      </EmptyState>
    );
  }

  if (error) {
    return (
      <ErrorState
        error={error}
        fallback="Não foi possível carregar o clima agora."
        onRetry={reload}
      />
    );
  }

  return (
    <div className={styles.page}>
      <section
        className={styles.hero}
        style={weather.imageUrl ? { backgroundImage: `url(${weather.imageUrl})` } : undefined}
      >
        <div className={styles.overlay}>
          <div>{backButton}</div>
          <div>
            <div className={styles.summary}>
              <img
                src={`https://openweathermap.org/img/wn/${weather.icon}@2x.png`}
                alt=""
                className={styles.icon}
              />
              <div>
                <Typography.Title level={1} className={styles.temperature}>
                  {weather.temperature}°C
                </Typography.Title>
                <Typography.Text className={styles.description}>
                  {weather.description}
                </Typography.Text>
              </div>
            </div>
            <Typography.Title level={2} className={styles.city}>
              {weather.city}, {weather.country}
            </Typography.Title>
          </div>
        </div>
      </section>

      <Row gutter={[16, 16]}>
        {toMetrics(weather).map((metric) => (
          <Col key={metric.label} {...METRIC_COLS}>
            <Card size="small" className={styles.metric}>
              <Statistic
                title={
                  <Space size="small">
                    {metric.icon}
                    {metric.label}
                  </Space>
                }
                value={metric.value}
              />
            </Card>
          </Col>
        ))}
      </Row>
    </div>
  );
}
```

- [x] **Passo 2: Criar `web/src/pages/WeatherDetails.module.css`**

```css
.page {
  display: flex;
  flex-direction: column;
  gap: var(--ant-margin-lg);
}

.hero {
  min-height: 20rem;
  border-radius: var(--ant-border-radius-lg);
  overflow: hidden;
  background: linear-gradient(135deg, var(--ant-color-primary), var(--ant-color-primary-active))
    center / cover no-repeat;
}

.overlay {
  min-height: inherit;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: var(--ant-margin);
  padding: var(--ant-padding-lg);
  background: linear-gradient(to top, var(--ant-color-bg-mask), transparent);
}

.summary {
  display: flex;
  align-items: center;
  gap: var(--ant-margin-sm);
}

.icon {
  width: 6rem;
  height: 6rem;
}

.hero .temperature,
.hero .city {
  color: var(--ant-color-white);
  margin: 0;
}

.hero .description {
  color: var(--ant-color-white);
  font-size: var(--ant-font-size-lg);
  text-transform: capitalize;
}

.heroSkeleton {
  min-height: 20rem;
}

.metric {
  height: 100%;
}
```

O `style` inline do hero só leva `backgroundImage` com a URL da API (valor dinâmico, FE-07); sem `imageUrl`, fica o gradiente do token primário.

- [x] **Passo 3: Remover o que saiu**

```bash
git rm -r web/src/components/Loading.jsx web/src/styles
```

Conferir: `grep -rn "Loading\"\|styles/\|style={{" web/src` → só o `style` do hero em `WeatherDetails.jsx`.

- [x] **Passo 4: Verificação completa.**

- [x] **Passo 5: Conferência manual**
  - Cidade existente (ex.: “Tóquio”): skeleton, depois hero com foto, nome traduzido do clima atual, nove métricas com ícones; 375 px → duas por linha.
  - Cidade inexistente (“Xyzabc”): estado vazio “Cidade "Xyzabc" não encontrada.” com Voltar.
  - Provider parado (Gateway de pé): 503 → `ErrorState` com “Código do erro” igual ao `X-Correlation-ID` da resposta (Network → Headers) e botão copiar.
  - Trocar de cidade pela busca rapidamente não mostra a cidade anterior.
  - Tema escuro: texto do hero legível sobre a foto.

- [x] **Passo 6: Commit**

```bash
git add web/src
git commit -m "Redesenha o detalhe do clima com hero, métricas em cards e estados de erro"
```

---

### Tarefa 7: Documentação, CI e fechamento da 07a

**Arquivos:**
- Modificar: `docs/architecture/frontend.md`, `docs/architecture/review-checklist.md`, `CLAUDE.md`, `docs/runbook.md`, `.github/workflows/ci.yml`, `docs/project-overview.md`, `docs/features/00-roteiro-portfolio/auditoria-inicial.md`, este `plan.md` (checkboxes + notas)

- [ ] **Passo 1: CI — `.github/workflows/ci.yml`**, no job Web, entre `npm run lint` e `npm run build`:

```yaml
      - run: npm test
```

- [ ] **Passo 2: `CLAUDE.md` — seção Verificação**

```bash
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web test
npm --prefix web run build
```

(manter a linha de `dotnet test` se já estiver lá; só acrescentar `npm --prefix web test` antes do build do web.)

- [ ] **Passo 3: `docs/runbook.md`**
  - Onde o runbook lista os comandos do web, acrescentar `npm --prefix web test` (Vitest, sem backend).
  - Na seção de CI: “**Web** (lint + testes + build)”.

- [ ] **Passo 4: `docs/architecture/frontend.md`**
  - **Stack:** acrescentar `Vitest + Testing Library (jsdom)`.
  - **Estrutura de pastas:** substituir a estrutura antiga e o bloco “Alvo (Fase 07)” pela estrutura real:

```
web/src/
├── main.jsx         ← AppProviders + AppRouter; importa antd/dist/reset.css
├── app/             ← AppProviders, AuthProvider/AuthContext, router, ProtectedRoute, GuestRoute
├── layouts/         ← AppLayout (header, conteúdo, footer), AuthLayout (tela dividida)
├── pages/           ← páginas finas: compõem componentes e chamam hooks
├── components/      ← reutilizáveis, cada um com seu .module.css quando precisar
├── hooks/           ← useAsync, useCities, useFavorites, useWeather, useApiFeedback
├── services/        ← client.js (axios) + auth.js, cities.js, weather.js, errors.js, session.js
├── theme/           ← tokens.js (claro/escuro), ThemeProvider/ThemeContext, themeStorage.js
├── utils/           ← funções puras (formatTime)
├── test/setup.js    ← jest-dom, matchMedia, limpeza entre testes
└── assets/
```

  - **Rotas:** remover `/signout`; acrescentar que `/signin` e `/signup` ficam sob `GuestRoute` (quem tem sessão vai para `/p`) e `AuthLayout`, e as rotas `/p/*` sob `ProtectedRoute` e `AppLayout`, aninhadas com `<Outlet/>`.
  - **Chamadas à API:** trocar `services/api.js` por `services/client.js` + um arquivo por recurso; `getApiErrorMessage` agora em `services/errors.js`; acrescentar: “Erro inesperado (sem resposta ou status ≥ 500) mostra o `X-Correlation-ID` (`getCorrelationId`) com `CorrelationId`; erro de negócio mostra só a mensagem — `useApiFeedback()` decide.”
  - **Tema e estilo:** tirar “(a partir da Fase 07)” do título; acrescentar: “`ConfigProvider` com `cssVar: true`: os `.module.css` usam `var(--ant-*)` (ex.: `--ant-color-primary`, `--ant-padding-lg`). Cores literais só em `theme/tokens.js`.” e “Feedback via `App.useApp()` (`message`, `notification`, `modal`); os estáticos do `antd` não herdam o tema.”
  - **Estado:** acrescentar “Carga de tela com `useAsync(fn)` (`{ data, isLoading, error, reload }`); `fn` estável (função de módulo ou `useCallback`). Lista mostra skeleton só sem dados (`isLoading && !data`), para o `reload` não piscar.”
  - **Nova seção Testes:** “Vitest + Testing Library (jsdom), `*.test.js(x)` ao lado do código. Toda lógica em `services/`, `hooks/`, `theme/` e `utils/` nasce com teste; componentes visuais não têm teste por ora.” e o comando `npm --prefix web test` na seção Lint (renomear para “Lint, testes e build”).

- [ ] **Passo 5: `docs/architecture/review-checklist.md`** — duas linhas novas no fim da tabela Frontend:

```markdown
| FE-12 | 🟡 | 07 | Lógica sem teste | Função ou hook novo/alterado em `services/`, `hooks/`, `theme/` ou `utils/` sem `*.test.js(x)` correspondente | [Testes](frontend.md#testes) |
| FE-13 | 🟡 | 07 | Feedback estático | `message.`/`notification.`/`Modal.confirm` importados de `antd` em vez de `App.useApp()` | [Tema e estilo](frontend.md#tema-e-estilo) |
```

E corrigir as âncoras `#tema-e-estilo-a-partir-da-fase-07` das linhas FE-05 a FE-08 para `#tema-e-estilo`.

- [ ] **Passo 6: `docs/features/00-roteiro-portfolio/auditoria-inicial.md`** — linha A27, coluna Status: `✅ URL na Fase 01 · resto na 07a`.

- [ ] **Passo 7: `docs/project-overview.md`**
  - Fase 07: título sem ✅ (a 07b falta); abaixo, como na Fase 03:

```markdown
Dividida em dois blocos:
- **07a — Fundação visual** ✅ (AAAA-MM-DD): tema Céu claro/escuro, AppLayout/AuthLayout, CSS Modules, skeletons, estados vazio/erro com correlation ID, Vitest. [Design](features/07-frontend-moderno/design.md) · [Plano](features/07-frontend-moderno/plan.md)
- **07b — Busca e previsão**: busca de cidades, previsão de 5 dias com gráfico, temperatura nos cards.
```

  - Auditoria da Fase 07: `A27 (resolvida na 07a)`.
  - **Estado atual:** data e “após a 07a”; acrescentar em Funciona “tema claro/escuro, layout responsivo, testes de lógica no web (Vitest)”; Stack ganha `Vitest · Testing Library`.
  - **Pendências:** remover a `[Fase 07]` do `CityCard.jsx`; manter as de busca e de nome da previsão (são da 07b); acrescentar `[Backlog] Testes de componentes do web (render de telas com Testing Library)`.

- [ ] **Passo 8: Verificação final** (skill `superpowers:verification-before-completion`)

```bash
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web test
npm --prefix web run build
```

Esperado: build .NET sem erros; lint limpo; todos os testes do web `passed` (~33); build do web `✓ built`. Conferir também `grep -rn "alert(\|confirm(\|#[0-9a-fA-F]\{3,6\}\b" web/src --include=*.jsx --include=*.css` → nenhum resultado fora de `theme/tokens.js`.

- [ ] **Passo 9: Notas de execução** — acrescentar no fim deste plano `## Notas de execução (AAAA-MM-DD)` com desvios e achados; marcar todos os checkboxes.

- [ ] **Passo 10: `/revisar-padrao`** no diff da branch inteira; com 🔴, parar. Depois `/requesting-code-review`.

- [ ] **Passo 11: Commit final**

```bash
git add CLAUDE.md .github/workflows/ci.yml docs
git commit -m "Documenta a fundação visual do web e fecha a Fase 07a"
```

Depois, `/finishing-a-development-branch` → PR `dev/frontend-moderno` → `master` citando **A27** e a pendência do `CityCard`. PR com CI ❌ não é mergeado.
