# Design — Fase 07b: Busca e previsão

**Data:** 2026-11-06
**Fase:** 07 (bloco **b** de 2)
**Status:** Aprovado
**Base:** [design 07a](design.md) · [roteiro](../00-roteiro-portfolio/design.md#07--frontend-moderno) · [design 03b](../03-comunicacao-servicos/endpoints-design.md) · [`frontend.md`](../../architecture/frontend.md) · [`backend.md`](../../architecture/backend.md)
**Branch:** `dev/busca-previsao`

---

## Objetivo

Fechar a Fase 07 com o que falta do roteiro: **busca de cidades** na Home, **previsão de 5 dias com gráfico** no detalhe e **temperatura nos cards**. Junto, resolver o nome da cidade em inglês ("Tóquio" → "Tokyo") que aparece no clima atual, na previsão e no histórico: o Provider passa a resolver a cidade pelo geocoding (nome em português + coordenadas) antes de consultar clima e previsão.

**Fora do escopo:**
- Autocomplete por tecla e troca de provedor de geocoding — a busca é ao confirmar.
- Desambiguação de homônimos ("Springfield"): a escolha continua enviando só o nome; vale a primeira cidade do geocoding (limitação registrada).
- Migração do histórico já gravado em inglês (só há dados de desenvolvimento).
- Mudança no contrato do Gateway (`/weathers`, `/forecasts`, `/locations`, `/cities` seguem iguais).

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Nome da cidade | **Provider resolve** a cidade no geocoding (primeiro resultado, `local_names.pt` ou o nome original, país, lat/lon) e consulta clima e previsão **por coordenadas** | Só o front mostrar o nome da busca (histórico seguiria em inglês); trocar só o nome mantendo `q=` (nome e dados podem ser de cidades diferentes) |
| Busca | Ao confirmar (Enter/Buscar) → lista com até 5 resultados (nome, estado, país) para escolher, mesmo com 1 resultado | Ir direto com 1 resultado; autocomplete com outro provedor (ADR) |
| Identificação | Só o nome, URL `/p/weather/:nome` (decisão da 03b) | Nome + país; coordenadas no contrato, na URL e na entidade `City` |
| Temperatura nos cards | Cada card busca o próprio clima (`GET /weathers/{nome}`), cache de 10 min do Provider segura a cota | `/cities` agregando temperatura no Gateway; sem temperatura |
| Gráfico | **Recharts** 3 (React 19 suportado), cores de `theme.useToken()` | `@ant-design/plots` (pesado); SVG próprio |
| Falha da previsão | Isolada na seção da previsão; o clima atual continua na tela | Página inteira em erro |

---

## 1. Backend — Provider

### Resolução da cidade

- `OpenWeatherLocationResponse` ganha `Lat` e `Lon` (já vêm no JSON do `geo/1.0/direct`).
- Novo **`CityResolver`** (`Application/Services`, contrato `ICityResolver`):
  - `ResolveAsync(string cityName, CancellationToken)` → `ResolvedCity?` com `Name` (português, via `local_names.pt`, senão o nome original), `OriginalName` (nome original do geocoding), `Country`, `Lat`, `Lon`.
  - Usa `IOpenWeatherClient.GetLocationsAsync` e o **primeiro** resultado; lista vazia → `null`.
  - Cache `IProviderCache` com prefixo `cidades` e TTL de 24 h (cidade não muda). Resultado vazio **não** é cacheado (evita gravar `null` para erro de digitação).
- A regra "nome em português ou original" passa a existir em um só lugar, usada também pelo `LocationAppService` (`/locations`).

### Clima e previsão por coordenadas

- `IOpenWeatherClient.GetCurrentAsync(lat, lon)` e `GetForecastAsync(lat, lon)` substituem as versões por nome (`data/2.5/weather?lat=&lon=` e `data/2.5/forecast?lat=&lon=`, mantendo `units=metric&lang=pt_br`). Coordenadas formatadas com `CultureInfo.InvariantCulture`.
- `WeatherAppService` e `ForecastAppService`:
  1. cache pelo nome pedido (como hoje);
  2. `CityResolver.ResolveAsync(nome)` → `null` → devolve `null` (404 no Gateway, como hoje);
  3. consulta por `Lat`/`Lon`;
  4. `City`/`Country` da resposta vêm do `ResolvedCity` (o `name` da OpenWeather por coordenadas pode ser um bairro);
  5. a imagem da Pexels usa `OriginalName` (busca em inglês dá resultados melhores, como hoje).
- Falha do geocoding segue o tratamento atual (`ExternalServiceUnavailableException` → 503).
- Custo: +1 chamada ao geocoding por cidade nova a cada 24 h.

### Efeito no Gateway

Nenhuma mudança de código: o `WeatherAppService` do Gateway grava no histórico o `weather.City`, que passa a vir em português. Histórico antigo em inglês continua como está (pode coexistir "Tokyo" e "Tóquio" para quem já buscou).

## 2. Front — busca de cidades (Home)

- `services/locations.js`: `searchLocations(query)` → `GET /locations?query=` (com `params`, o axios faz o encode).
- Hook **`useCitySearch()`** → `{ results, isSearching, search(query), clear() }`:
  - `search` ignora consulta com menos de 2 caracteres (após `trim`);
  - descarta resposta de busca anterior (mesma técnica do `useAsync`);
  - erro → `notifyError` (`useApiFeedback`) e `results` vazio.
- Componente **`CitySearch`**: `Input.Search` + painel abaixo (dentro do fluxo da página, não popover) com:
  - skeleton enquanto busca;
  - `List` com nome, estado e país; clique navega para `/p/weather/:nome`;
  - teclado: setas ↑/↓ movem a seleção, Enter abre, Esc limpa;
  - sem resultado: `EmptyState` “Nenhuma cidade encontrada para "<termo>".”
- Home troca o `Input.Search` atual pelo `CitySearch`.

## 3. Front — temperatura nos cards

- `CityCard` chama `useWeather(city.name)` e mostra ícone + temperatura arredondada no canto do card.
- Carregando: `Skeleton.Avatar`/`Skeleton.Input` pequeno. Erro: “—” com `Tooltip` “Clima indisponível” — sem `notification` por card.
- `GET /weathers/{nome}` também garante a cidade no histórico — para cards, a cidade já existe, então não há efeito colateral novo.

## 4. Front — previsão no detalhe

- `services/weather.js` ganha `getForecastByCity(cityName)` → `GET /forecasts/{nome}`.
- Hook **`useForecast(cityName)`** (fino sobre `useAsync`).
- Componente **`ForecastSection`** abaixo das métricas, título “Próximos 5 dias”:
  - carregando: skeleton do gráfico + cards;
  - erro: `ErrorState` da seção (com “Tentar de novo”), o resto da página segue;
  - **`ForecastChart`** (Recharts `ResponsiveContainer` + `ComposedChart`): linhas de mínima e máxima (°C, eixo esquerdo) e barras de chance de chuva (%, eixo direito); eixo X com `formatWeekday`; tooltip em português; cores de `theme.useToken()` (`colorPrimary`, `colorWarning`, `colorInfo`) — sem hex;
  - **`ForecastDays`**: 5 cards (dia, ícone, descrição, mín/máx, chuva) — também é a leitura acessível do gráfico (o gráfico ganha `aria-hidden`/`role="img"` com descrição resumida).
- Título da página continua com `weather.city` (agora em português).
- `utils/formatWeekday.js`: `formatWeekday("2026-11-06")` → “sex., 06/11” (`Intl.DateTimeFormat("pt-BR")`, data tratada como local, sem deslocamento de fuso).

## 5. Testes

| Alvo | Casos |
|---|---|
| `CityResolver` (xUnit) | nome em português quando há `local_names.pt`; nome original sem `pt`; lista vazia → `null` e nada no cache; cache hit não chama a OpenWeather |
| `WeatherAppService` / `ForecastAppService` (Provider) | resolve antes de consultar; cidade não resolvida → `null`; consulta por lat/lon; `City`/`Country` do resolvido; imagem pelo nome original |
| `LocationAppService` | continua devolvendo o nome em português (regra compartilhada) |
| `services/locations.js` | chama `/locations` com `query` |
| `useCitySearch` | ignora < 2 caracteres; devolve resultados; erro limpa resultados e notifica; descarta resposta antiga |
| `useForecast` | troca de cidade refaz a carga |
| `formatWeekday` | dia da semana e data em pt-BR, sem pular dia por fuso |

## 6. Documentação

| Arquivo | Mudança |
|---|---|
| `backend.md` | Clima e previsão resolvem a cidade no geocoding (`CityResolver`) e consultam por coordenadas; cache `cidades` 24 h |
| `frontend.md` | Busca ao confirmar com lista (`CitySearch`/`useCitySearch`); gráficos com Recharts e cores de `theme.useToken()`; remove “o consumo é da Fase 07b” |
| `review-checklist.md` | BE-28 cita também `lat`/`lon`; FE-06 vale para props de cor do Recharts (só token) |
| `project-overview.md` | 07b ✅ e **Fase 07 ✅ Concluída**; remove as pendências `[Fase 07b]` de busca e de nome; registra homônimos como limitação conhecida |

## Riscos

- **Geocoding não acha o que o `q=` achava** (grafias alternativas): o usuário vê 404. Mitigação: a busca da Home já usa o mesmo geocoding, então o que aparece na lista sempre resolve.
- **Primeiro resultado do geocoding ≠ cidade esperada** em homônimos: limitação aceita (decisão de identificar só pelo nome).
- **Peso do Recharts** no bundle (~100 kB gzip): aceitável para a vitrine; se incomodar, carregar a `ForecastSection` com `React.lazy`.
