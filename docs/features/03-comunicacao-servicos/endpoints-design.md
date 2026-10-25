# Design — Fase 03b: Novos endpoints

**Data:** 2026-10-20
**Fase:** 03 (bloco **b** de 2)
**Status:** Aprovado
**Base:** [design 03a](design.md) · [roteiro](../00-roteiro-portfolio/design.md#03--comunicação-entre-serviços) · [`backend.md`](../../architecture/backend.md)
**Branch:** `dev/novos-endpoints`

---

## Objetivo

Expor pelo Gateway (BFF) a **previsão de 5 dias** (OpenWeather `data/2.5/forecast`) e a **busca de cidades** para autocomplete (OpenWeather `geo/1.0/direct`), implementadas no Provider com os padrões da 03a: DTOs tipados, 404 → `null`, dependência fora → exceção tipada → 503, resiliência e cache. Ao fim deste bloco a Fase 03 é marcada `✅ Concluída`.

**Fora do escopo:**
- Autocomplete na busca e tela/gráfico de previsão no front → Fase 07 (o roteiro já os põe lá); a 03b é verificada pelo Scalar
- Testes automatizados → Fase 04
- Busca por coordenadas (`lat`/`lon`) e previsão horária → não há consumidor

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Front | Nenhuma mudança; consumo na Fase 07 | AutoComplete e lista de 5 dias mínimos agora |
| Formato da previsão | O **Provider agrega por dia** no fuso da cidade (mín/máx, condição do meio-dia, chance de chuva) | Repassar os 40 pontos de 3 h e agregar no front |
| Identificação da cidade | Por **nome**, como o clima atual (`q=`) | `lat`/`lon` vindos do autocomplete — muda o contrato do clima e o histórico |
| Chamadas à OpenWeather | Um typed client **`OpenWeatherClient`** com os três endpoints e o tratamento de erro único; os AppServices deixam de ter `HttpClient` | Cada AppService com seu typed client e o mesmo `try/catch` copiado |
| Cache | `WeatherCache` vira um **cache genérico** (`IProviderCache`) com prefixo e TTL por uso | Uma classe de cache por endpoint |
| Rotas | Recursos próprios: `/forecasts` e `/locations` nos dois serviços | `/weathers/{cidade}/forecast`; `/cities/search` (conflita com o histórico em `/cities`) |
| Busca sem resultado | 200 com lista vazia | 404 |

---

## 1. Contrato

### `GET /forecasts?cityName=` (Provider) e `GET /forecasts/{cityName}` (Gateway)

```json
{
  "city": "Tóquio",
  "country": "JP",
  "days": [
    {
      "date": "2026-10-20",
      "description": "céu limpo",
      "icon": "01d",
      "tempMin": 16.2,
      "tempMax": 22.8,
      "precipitationProbability": 20,
      "windSpeedKmh": 18.4
    }
  ]
}
```

- `days`: até 5 itens, um por data local da cidade, em ordem. O primeiro é hoje (parcial: só os horários restantes).
- `date`: `DateOnly` (`yyyy-MM-dd`) no fuso da cidade.
- `description`/`icon`: do ponto de 3 h mais próximo de 12:00 local.
- `tempMin`/`tempMax`: mínimo de `main.temp_min` e máximo de `main.temp_max` do dia.
- `precipitationProbability`: maior `pop` do dia × 100, inteiro (0–100).
- `windSpeedKmh`: maior `wind.speed` do dia × 3,6, uma casa.
- A previsão **não** grava no histórico do usuário (só a consulta de clima grava).

### `GET /locations?query=` (Provider e Gateway)

```json
[
  { "name": "São Paulo", "state": "São Paulo", "country": "BR" }
]
```

- Até 5 itens (`limit=5`), na ordem da OpenWeather.
- `name`: `local_names.pt` se existir, senão `name`.
- `state`: `null` quando a OpenWeather não informa.
- `query` com espaços aparados; menos de 2 ou mais de 100 caracteres → 400.

### Respostas de erro

| Situação | Provider | Gateway |
|---|---|---|
| `cityName` vazio (previsão) | 400 | — (rota exige o segmento) |
| `query` vazia, < 2 ou > 100 caracteres | 400 "Parâmetro 'query' deve ter entre 2 e 100 caracteres." | 400 (idem, validado no Gateway antes de chamar o Provider) |
| Cidade não existe (previsão) | 404 "Cidade não encontrada." | 404 |
| Busca sem resultado | 200 `[]` | 200 `[]` |
| OpenWeather fora/401/429/5xx/timeout/circuito | 503 "Serviço de clima indisponível." | 503 "Serviço de clima indisponível. Tente novamente em instantes." |
| Provider fora | — | 503 |
| Cliente abortou | 499 | 499 |

## 2. Provider

### Application
- **`Model/OpenWeather/OpenWeatherForecastResponse`**: `list[]` (`dt`, `main.temp_min/temp_max`, `weather[0].description/icon`, `wind.speed`, `pop`) e `city` (`name`, `country`, `timezone`).
- **`Model/OpenWeather/OpenWeatherLocationResponse`**: `name`, `local_names` (`IReadOnlyDictionary<string, string>?`), `country`, `state?`.
- **`Contracts/IOpenWeatherClient` + `Services/OpenWeatherClient`** (typed client, resiliência atual da OpenWeather 5 s/15 s/2 retries):
  - `GetCurrentAsync(cityName)` → `OpenWeatherCurrentResponse?`
  - `GetForecastAsync(cityName)` → `OpenWeatherForecastResponse?`
  - `GetLocationsAsync(query)` → `IReadOnlyList<OpenWeatherLocationResponse>`
  - Um método privado `GetAsync<T>(url)` concentra o que hoje está em `WeatherAppService.GetCurrentAsync`: 404 → `null`; outro status de erro, corpo vazio, `HttpRequestException`, `ExecutionRejectedException` ou `JsonException` → `ExternalServiceUnavailableException`. `units=metric&lang=pt_br` nos três.
- **`Services/WeatherAppService`**: passa a usar `IOpenWeatherClient` (sem `HttpClient` nem `apiKey`); comportamento igual.
- **`Contracts/IForecastAppService` + `Services/ForecastAppService.GetByCityAsync`** → `Task<Forecast?>`: cache → `GetForecastAsync` → `null` repassa → agrega (método privado, regras da seção 1) → grava no cache.
- **`Contracts/ILocationAppService` + `Services/LocationAppService.SearchAsync`** → `Task<IReadOnlyList<Location>>`: cache → `GetLocationsAsync` → mapeia → grava no cache (inclusive lista vazia).
- **`Contracts/IProviderCache` + `Services/ProviderCache`** (substitui `IWeatherCache`/`WeatherCache`): `GetAsync<T>(string prefix, string text)` / `SetAsync<T>(prefix, text, value, TimeSpan ttl)`. Mesma normalização de chave (`{prefixo}:{texto-normalizado}`), mesmo JSON e mesmo tratamento de falha (Warning e segue).

| Uso | Prefixo | TTL |
|---|---|---|
| Clima atual | `clima` | 10 min |
| Previsão | `previsao` | 30 min |
| Busca de cidades | `locais` | 24 h |

### Domain
- **`Weathers/Forecast`** (`City`, `Country`, `Days`) e **`Weathers/ForecastDay`** (campos da seção 1).
- **`Locations/Location`** (`Name`, `State?`, `Country`).

### Api
- **`ForecastController`** (`/forecasts`) e **`LocationController`** (`/locations`), no molde do `WeatherController`: validação do parâmetro com `Problem(400, ...)`, `null` → 404, sem `try/catch`, XML doc e `ProducesResponseType`.

## 3. Gateway

### Application
- **`Model/Weathers/ForecastDto` + `ForecastDayDto`** e **`Model/Locations/LocationDto`**: espelham a seção 1.
- **`IWeatherProviderClient`**: + `GetForecastAsync(cityName)` → `ForecastDto?` e `SearchLocationsAsync(query)` → `IReadOnlyList<LocationDto>`.
- **`IWeatherAppService`**: + `GetForecastAsync(cityName)` (repassa; não grava histórico).
- **`Contracts/ILocationAppService` + `Services/LocationAppService.SearchAsync`** (repassa).
- **`Model/Locations/LocationSearchQuery`** (`Query`) + **`Validators/LocationSearchQueryValidator`**: `Query` aparada com 2–100 caracteres, mensagem em português; roda pelo `ValidationFilter` como os DTOs de entrada.

### Infrastructure
- **`Http/WeatherProviderClient`**: os três métodos usam um `GetAsync<T>(url)` privado com o tratamento atual (404 → `null`; erro → `ProviderUnavailableException`). Resiliência inalterada (25 s/30 s, retry só de transporte).

### Api
- **`ForecastController`** (`GET /forecasts/{cityName}`) e **`LocationController`** (`GET /locations?query=` com `[FromQuery] LocationSearchQuery`), `[Authorize]`, XML doc.

## 4. Resiliência e cotas

- Sem mudança de timeouts: previsão e busca fazem **uma** chamada à OpenWeather cada (sem Pexels), abaixo do orçamento do clima atual.
- O autocomplete chama a cada digitação: o cache de 24 h e o mínimo de 2 caracteres reduzem o gasto; o *debounce* é do front (Fase 07, registrado como pendência).

## 5. Documentação

| Arquivo | Mudança |
|---|---|
| `backend.md` | Estrutura (Domain `Locations/`), `OpenWeatherClient` único, cache genérico com tabela prefixo/TTL, regra de agregação da previsão no Provider |
| `system-design.md` | Fluxos de previsão e busca; linhas novas no mapeamento de erros |
| `review-checklist.md` | Chamada à OpenWeather só pelo `IOpenWeatherClient` |
| `frontend.md` | Endpoints `/forecasts/{cidade}` e `/locations?query=` disponíveis para a Fase 07 |
| `project-overview.md` | 03b concluída; título da Fase 03 `✅ Concluída (AAAA-MM-DD)`; estado atual; pendência `[Fase 07]` de *debounce* no autocomplete |

## Verificação

Automática: seção [Verificação](../../../CLAUDE.md#verificação) (`dotnet build` sem avisos, lint e build do web).

Manual (Scalar):

1. `GET /forecasts/Tóquio` → 200 com 5 dias, datas no fuso de Tóquio, `precipitationProbability` entre 0 e 100, `windSpeedKmh` = maior `speed` × 3,6.
2. Mesma previsão de novo → não chama a OpenWeather (log do `HttpClient`); a cidade **não** aparece no histórico.
3. `GET /forecasts/CidadeInexistente` → 404 nos dois serviços.
4. `GET /locations?query=sao paulo` → até 5 itens com nome em português; `query=xyzxyzxyz` → 200 `[]`; `query=a` → 400.
5. Clima atual (`/weathers/{cidade}`) segue igual após a troca para `OpenWeatherClient` e `ProviderCache` (chave `clima:` mantida).
6. `OpenWeather:ApiKey` inválida → 503 em previsão e busca; Provider desligado → 503 no Gateway.
7. Sem token → 401 nos dois endpoints novos.

## Riscos

- **Nome ambíguo** ("Springfield"): previsão e clima usam a primeira cidade que a OpenWeather achar, como hoje. A busca mostra `state`/`country`, mas a seleção na Fase 07 ainda envia só o nome — reavaliar `lat`/`lon` se incomodar.
- **Primeiro e último dia parciais** na previsão (janela de 120 h da OpenWeather): aceito; `tempMin`/`tempMax` refletem só os horários disponíveis.
- **Renomear `WeatherCache` → `ProviderCache`** toca código da 03a: comportamento e chave do clima mantidos (item 5 da verificação).
