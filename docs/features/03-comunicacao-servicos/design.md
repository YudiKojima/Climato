# Design — Fase 03a: Contrato e robustez

**Data:** 2026-10-19
**Fase:** 03 (bloco **a** de 2)
**Status:** Aprovado
**Base:** [roteiro](../00-roteiro-portfolio/design.md#03--comunicação-entre-serviços) · [auditoria](../00-roteiro-portfolio/auditoria-inicial.md) · [pendências `[Fase 03]`](../../project-overview.md#pendências-futuras)
**Branch:** `dev/contrato-robustez`

---

## Objetivo

Trocar o parsing de JSON por dicionário por **contratos tipados**, fazer cada falha virar o status HTTP certo, proteger as chamadas entre serviços com **resiliência**, poupar a cota da OpenWeather com **cache** e expor **health checks**. Depois deste bloco, o clima mostrado é correto (vento em km/h, horários no fuso da cidade) e uma falha externa nunca aparece como "cidade não encontrada".

A Fase 03 foi dividida em dois blocos, cada um com design, plano e PR próprios:

| Bloco | Escopo | Documentos |
|---|---|---|
| **03a — Contrato e robustez** (este) | DTOs tipados, erros, resiliência, cache, health checks, unidades e fusos, pendências `[Fase 03]` | `design.md` · `plan.md` |
| **03b — Novos endpoints** | Previsão de 5 dias e busca de cidades (geocoding), no Provider e expostos pelo Gateway | `endpoints-design.md` · `endpoints-plan.md` |

A 03b depende do contrato e do mapeamento de erros desta. A fase só é marcada `✅ Concluída` no `project-overview.md` ao fim da 03b.

**Auditoria resolvida:** A14, A15, A16, A17, A19.

**Pendências `[Fase 03]` resolvidas:** checagens impossíveis (`?? throw` nos AppServices), `HttpResponseMessage`/`HttpRequestMessage` sem `using`, `CityRepository.UpdateAsync` com `Update()` redundante, supressões de nulabilidade CS8601/CS8604/CS8613, `OperationCanceledException` → 500, cadastro concorrente → 500, token de conta apagada → 500, título em inglês do 400 automático.

**Fora do escopo:**
- Previsão de 5 dias e geocoding → 03b
- Testes automatizados → Fase 04 (verificação manual, ver [Verificação](#verificação))
- Redis no `docker-compose` → Fase 05
- Logs estruturados (Serilog) → Fase 06
- Reorganização do front e `try/catch` do `CityCard` → Fase 07

## Decisões

| Tema | Decisão | Alternativa descartada |
|---|---|---|
| Divisão da fase | Dois blocos (03a contrato/robustez, 03b endpoints), um PR cada | Um PR único; três blocos |
| Vento e horários (A16, A17) | O **Provider** entrega pronto: `windSpeedKmh` e `sunrise`/`sunset` como `DateTimeOffset` no fuso da cidade | Dados brutos + `utcOffsetSeconds` convertidos no front |
| Cache | `IDistributedCache`: Redis se `ConnectionStrings:Redis` estiver configurada, senão memória | Redis obrigatório; memória até a Fase 05 |
| Erros | Cidade inexistente = fluxo esperado → `null` → 404. Dependência fora = exceção tipada → `GlobalExceptionHandler` → 503 | Result pattern; repassar o status do Provider |
| Status do Provider em falha externa | Sempre **503** | Distinguir 502 (resposta inválida) de 503 (timeout/circuito) — o Gateway responde 503 de qualquer jeito |
| Resiliência | `AddStandardResilienceHandler()` com timeouts escalonados (Gateway > Provider) | Polly manual; padrão sem ajuste (retries multiplicados) |
| Health de APIs externas | Fora do `/health/ready` | Sondar OpenWeather/Pexels (gasta cota) |
| Mapeamento no Provider | Manual (OpenWeather → `Weather`) | Mapster — só no Gateway ([ADR 0005](../../architecture/decisions/0005-mapster.md)) |

---

## 1. Contrato

### `GET /weathers?cityName=` (Provider) e `GET /weathers/{cityName}` (Gateway)

Mesmo formato nos dois serviços (camelCase):

```json
{
  "city": "Tóquio",
  "country": "JP",
  "description": "céu limpo",
  "icon": "01d",
  "temperature": 21.4,
  "feelsLike": 20.9,
  "tempMin": 19.8,
  "tempMax": 22.6,
  "humidity": 60,
  "pressure": 1015,
  "windSpeedKmh": 14.8,
  "windDirection": 220,
  "sunrise": "2026-10-19T05:52:00+09:00",
  "sunset": "2026-10-19T17:08:00+09:00",
  "imageUrl": "https://images.pexels.com/..."
}
```

- `windSpeed` → **`windSpeedKmh`**: quebra intencional do contrato, para a unidade ficar no nome.
- `sunrise`/`sunset`: ISO 8601 com o offset da cidade.
- `imageUrl`: `null` quando o Pexels não acha foto ou falha.

### Respostas de erro

| Situação | Provider | Gateway |
|---|---|---|
| `cityName` vazio | 400 ProblemDetails | — (rota exige o segmento) |
| Cidade não existe na OpenWeather | 404 "Cidade não encontrada." | 404 "Cidade não encontrada." (não grava no histórico) |
| OpenWeather 401/429/5xx, rede, timeout, circuito aberto | 503 "Serviço de clima indisponível." | 503 "Serviço de clima indisponível. Tente novamente em instantes." |
| Pexels falha | 200 com `imageUrl: null` | 200 |
| Provider fora, timeout ou circuito aberto | — | 503 (idem) |
| Cliente abortou a requisição | 499, sem corpo | 499, sem corpo |

Todas as respostas de erro em `application/problem+json`.

## 2. Provider

### Application
- **`Model/OpenWeather/OpenWeatherCurrentResponse`** e **`Model/Pexels/PexelsSearchResponse`**: records com só os campos usados (`name`, `timezone`, `sys.country/sunrise/sunset`, `weather[0].description/icon`, `main.*`, `wind.speed/deg`; `photos[0].src.large2x`). Lidos com `ReadFromJsonAsync<T>`; OpenWeather com `JsonNamingPolicy.SnakeCaseLower`.
- **`Services/WeatherAppService.GetByCityAsync`** → `Task<Weather?>`:
  1. Consulta o `WeatherCache`; hit → devolve.
  2. Chama `data/2.5/weather`. `404` → `null` (não cacheia). Outro status de erro → `ExternalServiceUnavailableException`.
  3. Mapeia para `Weather` num método privado: `WindSpeedKmh = Math.Round(speed * 3.6, 1)`; `Sunrise = DateTimeOffset.FromUnixTimeSeconds(sunrise).ToOffset(TimeSpan.FromSeconds(timezone))` (idem `Sunset`).
  4. Busca a imagem, grava no cache e devolve.
  - `HttpRequestException` e `ExecutionRejectedException` (timeout/circuito do Polly) → `ExternalServiceUnavailableException` com a exceção original como `InnerException`.
- **`Services/CityImageAppService.GetImageUrlAsync`**: status de erro, `HttpRequestException` ou `ExecutionRejectedException` → log `Warning` e `null`. `OperationCanceledException` do cliente não é capturada.
- **`Services/WeatherCache`** (`IWeatherCache`): `GetAsync(cityName)` / `SetAsync(cityName, weather)` sobre `IDistributedCache`.
  - Chave `clima:{normalizada}` — `Trim`, espaços internos colapsados, `ToLowerInvariant`, sem diacríticos (`NormalizationForm.FormD` removendo `NonSpacingMark`).
  - Valor serializado com System.Text.Json; `AbsoluteExpirationRelativeToNow` = 10 min.
  - Exceção do cache (Redis fora) → log `Warning` e segue como miss / sem gravar.
- **`Exceptions/ExternalServiceUnavailableException`**.
- `using` em todo `HttpRequestMessage`/`HttpResponseMessage`.
- **Removidos:** `Core/Collections/JsonDictionary.cs`, `Core/Collections/HttpResponseMessageExtensions.cs`, pacote `Newtonsoft.Json`.
- **`DependencyInjection.AddProviderApplication(IConfiguration)`**: passa a receber a configuração para escolher o cache (`AddStackExchangeRedisCache` ou `AddDistributedMemoryCache`) e registrar `IWeatherCache` (singleton).

### Domain
- **`Weather`**: propriedades `required`, exceto `ImageUrl` (`string?`); `WindSpeed` → `WindSpeedKmh`; `Sunrise`/`Sunset` → `DateTimeOffset`.

### Api
- **`Errors/GlobalExceptionHandler`**: `ExternalServiceUnavailableException` → 503 com log `Warning`; `OperationCanceledException` com `RequestAborted` cancelado → 499 com log `Information`; resto → 500 "Erro interno." com log `Error`.
- **`WeatherController`**: `cityName` vazio → `Problem(statusCode: 400, title: "Parâmetro 'cityName' é obrigatório.")`; `null` → `Problem(statusCode: 404, title: "Cidade não encontrada.")`. Sem `try/catch`.
- **`Program.cs`**: `AddProblemDetails`, `AddExceptionHandler<GlobalExceptionHandler>`, health checks; pipeline `UseExceptionHandler → UseStatusCodePages → UseHttpsRedirection → MapControllers`, mais `MapHealthChecks`.

### Configuração
- `appsettings.json`: `ConnectionStrings:Redis` = `""` (vazio = cache em memória).

## 3. Gateway

### Application
- **`Model/Weathers/WeatherDto`**: espelha o contrato da seção 1 (`required`, `DateTimeOffset`, `WindSpeedKmh`).
- **`IWeatherProviderClient.GetWeatherAsync`** → `Task<WeatherDto?>`.
- **`IWeatherAppService.GetByCityAsync`** → `Task<WeatherDto?>`; `null` → não grava no histórico.
- **`Exceptions/ProviderUnavailableException`**.
- Construtores dos AppServices sem `?? throw ArgumentNullException`.

### Domain
- **`Exceptions/UserNotFoundException`**: usuário do token não existe mais.

### Infrastructure
- **`Http/WeatherProviderClient`**: `ReadFromJsonAsync<WeatherDto>()`; `404` → `null`; outro status de erro, `HttpRequestException` ou `ExecutionRejectedException` → `ProviderUnavailableException`. `using` na resposta.
- **`Http/ProviderHealthCheck`** (`IHealthCheck`): `GET health/ready` no Provider por um client nomeado **sem** resiliência, timeout de 5 s.
- **`Data/Repositories/UserRepository.CreateAsync`**: `DbUpdateException` com `PostgresException` `23505` em `ix_user_email` → `DomainException("E-mail já cadastrado.")` (409).
- **`Data/Repositories/CityRepository.CreateAsync`**: `PostgresException` `23503` em `fk_city_user` → `UserNotFoundException`.
- **`CityRepository.UpdateAsync`**: sem `Update()` — só `SaveChangesAsync`.
- **Removidos:** `Http/JsonDictionary.cs`, `Http/HttpResponseMessageExtensions.cs`, pacote `Newtonsoft.Json`.

### Api
- **`Errors/GlobalExceptionHandler`**: + `ProviderUnavailableException` → 503 (log `Warning`); `UserNotFoundException` → 401 "Sessão inválida." (log `Information`); `OperationCanceledException` com `RequestAborted` cancelado → 499 (log `Information`).
- **`WeatherController`**: `null` → `Problem(statusCode: 404, title: "Cidade não encontrada.")`.
- **`DependencyInjection.AddGatewayApi`**: `ConfigureApiBehaviorOptions` com `InvalidModelStateResponseFactory` gerando o mesmo `ValidationProblemDetails` do `ValidationFilter` (título "Dados inválidos.").
- **`Program.cs`**: `MapHealthChecks` (seção 6).

## 4. Resiliência

`AddStandardResilienceHandler()` nos três typed clients (retry com backoff exponencial, circuit breaker, timeouts; só repete em 408, 429, 5xx e falha de rede — nunca em 404/401). Valores fixos no `DependencyInjection.cs`:

| Client | Timeout por tentativa | Timeout total | Retries |
|---|---|---|---|
| Provider → OpenWeather | 5 s | 15 s | 2 |
| Provider → Pexels | 2 s | 5 s | 1 |
| Gateway → Provider | 25 s | 30 s | 1 (só falha de transporte) |

Regra: a tentativa de quem chama é maior que a soma dos timeouts totais das dependências que o chamado consulta em sequência. A tentativa do Gateway (25 s) é maior que OpenWeather (15 s) + Pexels (5 s) + folga do cache: o Gateway não corta o Provider no meio de um retry, e o pior caso fica em ~6 chamadas à OpenWeather em vez de 16. O Gateway só re-tenta falha de transporte (`HttpRequestException`, ex.: Provider fora/conexão recusada); 5xx do Provider, que já fez seus próprios retries, não é re-tentado. A janela de amostragem do circuit breaker precisa ser ≥ 2× o timeout por tentativa (validação do pacote) — ajustar junto.

## 5. Cache

Ver `WeatherCache` na seção 2. Resumo:

| | |
|---|---|
| Implementação | Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`) se `ConnectionStrings:Redis` preenchida; senão `AddDistributedMemoryCache` |
| Chave | `clima:{cidade-normalizada}` — "Cuiabá", " cuiaba " e "CUIABÁ" → `clima:cuiaba` |
| Valor | `Weather` completo (com `imageUrl`), JSON |
| TTL | 10 min, absoluto |
| Não cacheia | Cidade inexistente (404), falhas |
| Redis fora | Warning no log, segue sem cache; aparece como `Degraded` no `/health/ready` |

## 6. Health checks

| Serviço | `/health/live` | `/health/ready` |
|---|---|---|
| Provider | nenhum check (processo vivo) | Redis, só se configurado (`AspNetCore.HealthChecks.Redis`), com `failureStatus: Degraded` (responde 200; Redis fora não derruba o `ready` do Gateway) |
| Gateway | nenhum check | Postgres (`AddDbContextCheck<ClimatoDbContext>`) + `ProviderHealthCheck` |

- `live` usa `Predicate = _ => false`; `ready` filtra por tag `ready`.
- Gateway: `MapHealthChecks(...).AllowAnonymous()`.
- Resposta padrão em texto (`Healthy`/`Degraded`/`Unhealthy`).
- OpenWeather e Pexels **não** entram (gastariam cota; a falha já vira 503 ou `imageUrl: null` no fluxo).

## 7. Pacotes

| Pacote | Projeto |
|---|---|
| `Microsoft.Extensions.Http.Resilience` | `Provider.Application`, `Gateway.Infrastructure` |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | `Provider.Application` |
| `AspNetCore.HealthChecks.Redis` | `Provider.Api` |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | `Gateway.Infrastructure` |
| ~~`Newtonsoft.Json`~~ (removido) | ambos e `Directory.Packages.props` |

Versões só no `Directory.Packages.props`. Conferir o `.csproj` antes de usar cada namespace.

## 8. Nulabilidade

- Remover do `.editorconfig` as supressões CS8601, CS8604 e CS8613.
- `dotnet build Climato.slnx` sem nenhum aviso de nulabilidade (os de `WeatherDto`/`Weather`/`JsonDictionary` somem com os DTOs tipados; os restantes são corrigidos).

## 9. Frontend (ajuste mínimo)

- **`pages/WeatherDetails.jsx`**:
  - `weather.windSpeed` → `weather.windSpeedKmh` (A16).
  - `catch`: 404 → estado "cidade não encontrada"; qualquer outro erro → `message.error(getApiErrorMessage(err))` (A15).
- **`components/formatDatetime.jsx`**: extrai `HH:mm` direto da string ISO (já no fuso da cidade), sem `new Date().toLocaleTimeString()`, que reconverte para o fuso do navegador (A17).

## 10. Documentação

| Arquivo | Mudança |
|---|---|
| `backend.md` | Stack (JSON, resiliência, cache); remover marcações "(a partir da Fase 03)"; regra "dependência fora → exceção tipada → 503, cidade inexistente → `null` → 404"; seções de cache e health checks; `AddProviderApplication(configuration)` |
| `system-design.md` | Tabela de mapeamento de erros (Provider sempre 503; 499); chave do cache; health checks |
| `runbook.md` | Redis opcional (`docker run -d -p 6379:6379 redis:7-alpine` + `ConnectionStrings:Redis`); URLs `/health/live` e `/health/ready` |
| `review-checklist.md` | Sem `Dictionary`/Newtonsoft para JSON externo; typed client com `AddStandardResilienceHandler`; `using` em `HttpResponseMessage`; falha de dependência → exceção tipada |
| `auditoria-inicial.md` | A14–A17, A19 → `✅ Fase 03` |
| `project-overview.md` | Divisão 03a/03b; pendências `[Fase 03]` resolvidas removidas; fase segue em andamento até a 03b |

## Verificação

Automática (seção [Verificação](../../../CLAUDE.md#verificação)):

```bash
dotnet build Climato.slnx
npm --prefix web run lint
npm --prefix web run build
```

Manual (Scalar + navegador):

1. Build sem avisos de nulabilidade.
2. Cidade válida → 200; `windSpeedKmh` = `speed` da OpenWeather × 3,6.
3. Tóquio: nascer/pôr do sol com `+09:00` na API e exibidos no horário local de Tóquio no front.
4. "Cuiabá" e depois "cuiaba" → a segunda não chama a OpenWeather (log do `HttpClient`).
5. Cidade inexistente → 404 nos dois serviços; não entra no histórico; front mostra "cidade não encontrada".
6. `OpenWeather:ApiKey` inválida → Provider 503, Gateway 503; front mostra a mensagem de indisponibilidade.
7. Provider desligado → Gateway 503 (após o retry) e `/health/ready` do Gateway `Unhealthy`.
8. `Pexels:ApiKey` inválida → 200 com `imageUrl: null` e warning no log.
9. Com Redis configurado e depois parado → clima continua respondendo; `/health/ready` do Provider `Degraded` (200) e o do Gateway segue `Healthy`.
10. JSON malformado em `POST /auth/login` → 400 com título "Dados inválidos.".
11. Token de usuário apagado em `GET /weathers/{cidade}` → 401 e o front volta para `/signin`.
12. `/health/live` e `/health/ready` acessíveis sem token.

## Riscos

- **Quebra de contrato (`windSpeedKmh`, `DateTimeOffset`)** — front ajustado no mesmo PR; não há outros consumidores.
- **Cache em memória por instância** quando sem Redis — aceitável em dev; o compose da Fase 05 traz o Redis.
- **Gateway só re-tenta falha de transporte; 5xx do Provider não é re-tentado** — o Provider já fez seus próprios retries; com o Provider fora há 1 retry rápido (conexão recusada) e o circuit breaker corta as chamadas seguintes.
- **Sem testes automatizados** até a Fase 04 — os cenários acima viram os testes de integração (WireMock) da Fase 04.
