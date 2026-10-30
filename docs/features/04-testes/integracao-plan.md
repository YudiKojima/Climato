# Fase 04 — Testes de integração: Plano adiado

> **Status: adiado (`[Backlog]`).** Escrito junto com o plano da Fase 04 e retirado dela quando a fase foi reduzida a testes unitários (2026-10-27) para acelerar a conclusão do roteiro. Antes de executar, revisar versões de pacote e as decisões do [design](design.md#testes-de-integração-adiados); o restante (convenções, ajustes descobertos nos spikes) está no [plano da Fase 04](plan.md).

**Pacotes a acrescentar ao `Directory.Packages.props`:**

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.13" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="4.16.0" />
    <PackageVersion Include="Testcontainers.Redis" Version="4.16.0" />
    <PackageVersion Include="WireMock.Net" Version="2.20.0" />
```

**Achados dos spikes (2026-10-26) que valem para estas tarefas:**
- A fixture do Gateway sobe o Postgres **e o host** (`StartServer()`) no `InitializeAsync`; um segundo host iniciado em paralelo rodava as migrations ao mesmo tempo e falhava.
- "Provider fora" = host derivado com `Provider:BaseUrl` = `http://127.0.0.1:1/`; o `FaultType.EMPTY_RESPONSE` do WireMock vira 200 sem corpo (`JsonException`), não falha de transporte.
- Delay do retry zerado com `ConfigureAll<HttpStandardResilienceOptions>` nos dois factories.
- Rota sem stub no WireMock responde 404 (Pexels sem stub → clima sem imagem; Provider sem stub → cidade não encontrada).

### Tarefa I1: Provider — integração (WireMock + Redis)

**Files:**
- Create: `tests/Climato.Provider.IntegrationTests/Climato.Provider.IntegrationTests.csproj`, `Infrastructure/ProviderApiFactory.cs`, `Infrastructure/RedisFixture.cs`, `Infrastructure/ExternalApiStubs.cs`, `Api/WeatherEndpointTests.cs`, `Api/ForecastEndpointTests.cs`, `Api/LocationEndpointTests.cs`, `Api/HealthEndpointTests.cs`, `Api/RedisCacheTests.cs`
- Modify: `Climato.slnx`

**Interfaces:**
- Produces: `ProviderApiFactory` (class fixture; propriedade `ExternalApis` = `WireMockServer`), `ExternalApiStubs` (extensões de `WireMockServer`), `RedisFixture`.

- [ ] **Passo 1: `.csproj`** e linha no `Climato.slnx` (pasta `/Tests/`)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Testcontainers.Redis" />
    <PackageReference Include="WireMock.Net" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Provider\Climato.Provider.Api\Climato.Provider.Api.csproj" />
  </ItemGroup>

</Project>
```

```xml
    <Project Path="tests/Climato.Provider.IntegrationTests/Climato.Provider.IntegrationTests.csproj" />
```

- [ ] **Passo 2: `Infrastructure/ProviderApiFactory.cs`**

Uma instância por classe de teste (`IClassFixture`): WireMock e cache em memória próprios da classe. Dentro da classe, cada teste usa uma cidade diferente.

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using WireMock.Server;

namespace Climato.Provider.IntegrationTests.Infrastructure;

public sealed class ProviderApiFactory : WebApplicationFactory<Program>
{
    public const string OpenWeatherApiKey = "chave-openweather-de-teste";
    public const string PexelsApiKey = "chave-pexels-de-teste";

    public WireMockServer ExternalApis { get; } = WireMockServer.Start();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("OpenWeather:BaseUrl", $"{ExternalApis.Url}/");
        builder.UseSetting("OpenWeather:ApiKey", OpenWeatherApiKey);
        builder.UseSetting("Pexels:BaseUrl", $"{ExternalApis.Url}/");
        builder.UseSetting("Pexels:ApiKey", PexelsApiKey);
        builder.ConfigureServices(services =>
            services.ConfigureAll<HttpStandardResilienceOptions>(options => options.Retry.Delay = TimeSpan.Zero));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        ExternalApis.Stop();
    }
}
```

- [ ] **Passo 3: `Infrastructure/RedisFixture.cs`**

```csharp
using Testcontainers.Redis;

namespace Climato.Provider.IntegrationTests.Infrastructure;

public sealed class RedisFixture : IAsyncLifetime
{
    public RedisContainer Container { get; } = new RedisBuilder("redis:7-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
```

- [ ] **Passo 4: `Infrastructure/ExternalApiStubs.cs`**

```csharp
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Climato.Provider.IntegrationTests.Infrastructure;

public static class ExternalApiStubs
{
    public const string CurrentWeatherPath = "/data/2.5/weather";
    public const string ForecastPath = "/data/2.5/forecast";
    public const string LocationsPath = "/geo/1.0/direct";
    public const string ImagePath = "/v1/search";

    public static void StubCurrentWeather(this WireMockServer server, string cityName, int statusCode = 200, string? body = null)
    {
        server.Given(Request.Create().WithPath(CurrentWeatherPath).WithParam("q", cityName).UsingGet())
            .RespondWith(Json(statusCode, body ?? CurrentWeatherJson(cityName)));
    }

    public static void StubForecast(this WireMockServer server, string cityName, int statusCode = 200)
    {
        server.Given(Request.Create().WithPath(ForecastPath).WithParam("q", cityName).UsingGet())
            .RespondWith(Json(statusCode, ForecastJson(cityName)));
    }

    public static void StubLocations(this WireMockServer server, string query, string body)
    {
        server.Given(Request.Create().WithPath(LocationsPath).WithParam("q", query).UsingGet())
            .RespondWith(Json(200, body));
    }

    public static void StubCityImage(this WireMockServer server, string cityName, int statusCode = 200)
    {
        var body = $$"""{ "photos": [ { "src": { "large2x": "https://images.pexels.com/{{cityName}}.jpg" } } ] }""";

        server.Given(Request.Create().WithPath(ImagePath).WithParam("query", cityName).UsingGet())
            .RespondWith(Json(statusCode, body));
    }

    public static int CountRequests(this WireMockServer server, string path, string paramName, string paramValue)
    {
        return server.FindLogEntries(Request.Create().WithPath(path).WithParam(paramName, paramValue)).Count;
    }

    private static IResponseBuilder Json(int statusCode, string body)
    {
        return Response.Create()
            .WithStatusCode(statusCode)
            .WithHeader("Content-Type", "application/json")
            .WithBody(body);
    }

    private static string CurrentWeatherJson(string cityName)
    {
        var sunrise = new DateTimeOffset(2026, 10, 26, 10, 45, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var sunset = new DateTimeOffset(2026, 10, 26, 22, 50, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        return $$"""
            {
              "name": "{{cityName}}",
              "timezone": -18000,
              "sys": { "country": "EC", "sunrise": {{sunrise}}, "sunset": {{sunset}} },
              "weather": [ { "description": "nublado", "icon": "04d" } ],
              "main": { "temp": 14.2, "feels_like": 13.8, "temp_min": 12.0, "temp_max": 16.5, "humidity": 77, "pressure": 1022 },
              "wind": { "speed": 3.0, "deg": 90 }
            }
            """;
    }

    private static string ForecastJson(string cityName)
    {
        var morning = new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var midday = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        return $$"""
            {
              "city": { "name": "{{cityName}}", "country": "UY", "timezone": 0 },
              "list": [
                { "dt": {{morning}}, "main": { "temp_min": 14.0, "temp_max": 18.0 }, "weather": [ { "description": "nublado", "icon": "04d" } ], "wind": { "speed": 2.0 }, "pop": 0.1 },
                { "dt": {{midday}}, "main": { "temp_min": 15.0, "temp_max": 22.0 }, "weather": [ { "description": "céu limpo", "icon": "01d" } ], "wind": { "speed": 4.0 }, "pop": 0.25 }
              ]
            }
            """;
    }
}
```

- [ ] **Passo 5: `Api/WeatherEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Provider.Domain.Weathers;
using Climato.Provider.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Provider.IntegrationTests.Api;

public class WeatherEndpointTests(ProviderApiFactory factory) : IClassFixture<ProviderApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_OpenWeatherEPexelsRespondem_Devolve200ComImagem()
    {
        factory.ExternalApis.StubCurrentWeather("Quito");
        factory.ExternalApis.StubCityImage("Quito");

        using var response = await client.GetAsync("/weathers?cityName=Quito", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var weather = await response.Content.ReadFromJsonAsync<Weather>(cancellationToken);
        weather.ShouldNotBeNull();
        weather.City.ShouldBe("Quito");
        weather.Country.ShouldBe("EC");
        weather.WindSpeedKmh.ShouldBe(10.8);
        weather.Sunrise.Offset.ShouldBe(TimeSpan.FromHours(-5));
        weather.ImageUrl.ShouldBe("https://images.pexels.com/Quito.jpg");
        factory.ExternalApis.CountRequests(ExternalApiStubs.CurrentWeatherPath, "appid", ProviderApiFactory.OpenWeatherApiKey).ShouldBe(1);
    }

    [Fact]
    public async Task Get_PexelsFalha_Devolve200SemImagem()
    {
        factory.ExternalApis.StubCurrentWeather("Lima");
        factory.ExternalApis.StubCityImage("Lima", statusCode: 500);

        using var response = await client.GetAsync("/weathers?cityName=Lima", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var weather = await response.Content.ReadFromJsonAsync<Weather>(cancellationToken);
        weather.ShouldNotBeNull();
        weather.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Get_OpenWeatherDevolve404_Devolve404()
    {
        factory.ExternalApis.StubCurrentWeather("Atlantida", statusCode: 404, body: """{ "cod": "404" }""");

        using var response = await client.GetAsync("/weathers?cityName=Atlantida", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("Cidade não encontrada.");
    }

    [Fact]
    public async Task Get_OpenWeatherDevolve500_Devolve503AposTresTentativas()
    {
        factory.ExternalApis.StubCurrentWeather("Bogota", statusCode: 500, body: "{}");

        using var response = await client.GetAsync("/weathers?cityName=Bogota", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("Serviço de clima indisponível.");
        factory.ExternalApis.CountRequests(ExternalApiStubs.CurrentWeatherPath, "q", "Bogota").ShouldBe(3);
    }

    [Fact]
    public async Task Get_OpenWeatherDevolveJsonInvalido_Devolve503()
    {
        factory.ExternalApis.StubCurrentWeather("Caracas", body: "{ invalido");

        using var response = await client.GetAsync("/weathers?cityName=Caracas", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Get_CityNameVazio_Devolve400()
    {
        using var response = await client.GetAsync("/weathers?cityName=", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_MesmaCidadeDuasVezes_ChamaAOpenWeatherUmaVez()
    {
        factory.ExternalApis.StubCurrentWeather("Santiago");
        factory.ExternalApis.StubCityImage("Santiago");

        using var first = await client.GetAsync("/weathers?cityName=Santiago", cancellationToken);
        using var second = await client.GetAsync("/weathers?cityName=Santiago", cancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.ExternalApis.CountRequests(ExternalApiStubs.CurrentWeatherPath, "q", "Santiago").ShouldBe(1);
    }
}
```

- [ ] **Passo 6: `Api/ForecastEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Provider.Domain.Weathers;
using Climato.Provider.IntegrationTests.Infrastructure;

namespace Climato.Provider.IntegrationTests.Api;

public class ForecastEndpointTests(ProviderApiFactory factory) : IClassFixture<ProviderApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_OpenWeatherResponde_Devolve200AgregadoPorDia()
    {
        factory.ExternalApis.StubForecast("Montevideu");

        using var response = await client.GetAsync("/forecasts?cityName=Montevideu", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var forecast = await response.Content.ReadFromJsonAsync<Forecast>(cancellationToken);
        forecast.ShouldNotBeNull();
        forecast.Country.ShouldBe("UY");
        var day = forecast.Days.ShouldHaveSingleItem();
        day.Date.ShouldBe(new DateOnly(2026, 10, 26));
        day.TempMin.ShouldBe(14.0);
        day.TempMax.ShouldBe(22.0);
        day.Description.ShouldBe("céu limpo");
        day.PrecipitationProbability.ShouldBe(25);
        day.WindSpeedKmh.ShouldBe(14.4);
    }

    [Fact]
    public async Task Get_OpenWeatherDevolve404_Devolve404()
    {
        factory.ExternalApis.StubForecast("Atlantida", statusCode: 404);

        using var response = await client.GetAsync("/forecasts?cityName=Atlantida", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_OpenWeatherDevolve500_Devolve503()
    {
        factory.ExternalApis.StubForecast("Bogota", statusCode: 500);

        using var response = await client.GetAsync("/forecasts?cityName=Bogota", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        factory.ExternalApis.CountRequests(ExternalApiStubs.ForecastPath, "q", "Bogota").ShouldBe(3);
    }
}
```

- [ ] **Passo 7: `Api/LocationEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Provider.Domain.Locations;
using Climato.Provider.IntegrationTests.Infrastructure;

namespace Climato.Provider.IntegrationTests.Api;

public class LocationEndpointTests(ProviderApiFactory factory) : IClassFixture<ProviderApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_OpenWeatherEncontra_DevolveNomeEmPortugues()
    {
        factory.ExternalApis.StubLocations("Lisboa", """[ { "name": "Lisbon", "local_names": { "pt": "Lisboa", "en": "Lisbon" }, "country": "PT", "state": "Lisboa" } ]""");

        var locations = await client.GetFromJsonAsync<List<Location>>("/locations?query=Lisboa", cancellationToken);

        locations.ShouldNotBeNull();
        var location = locations.ShouldHaveSingleItem();
        location.Name.ShouldBe("Lisboa");
        location.Country.ShouldBe("PT");
    }

    [Fact]
    public async Task Search_SemResultado_Devolve200ComListaVazia()
    {
        factory.ExternalApis.StubLocations("Xyzw", "[]");

        using var response = await client.GetAsync("/locations?query=Xyzw", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var locations = await response.Content.ReadFromJsonAsync<List<Location>>(cancellationToken);
        locations.ShouldNotBeNull();
        locations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_QueryCurta_Devolve400()
    {
        using var response = await client.GetAsync("/locations?query=a", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Passo 8: `Api/HealthEndpointTests.cs`**

```csharp
using System.Net;
using Climato.Provider.IntegrationTests.Infrastructure;

namespace Climato.Provider.IntegrationTests.Api;

public class HealthEndpointTests(ProviderApiFactory factory) : IClassFixture<ProviderApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Get_SemRedis_DevolveHealthy(string path)
    {
        using var response = await client.GetAsync(path, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("Healthy");
    }
}
```

- [ ] **Passo 9: `Api/RedisCacheTests.cs`**

```csharp
using System.Net;
using Climato.Provider.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using StackExchange.Redis;

namespace Climato.Provider.IntegrationTests.Api;

public class RedisCacheTests(ProviderApiFactory factory, RedisFixture redis) : IClassFixture<ProviderApiFactory>, IClassFixture<RedisFixture>
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetWeather_ComRedis_GravaAChaveDeClimaComTtlDeDezMinutos()
    {
        await using var app = CreateAppWithRedis();
        factory.ExternalApis.StubCurrentWeather("Asuncion");
        factory.ExternalApis.StubCityImage("Asuncion");

        using var response = await app.CreateClient().GetAsync("/weathers?cityName=Asuncion", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var connection = await ConnectionMultiplexer.ConnectAsync(redis.Container.GetConnectionString());
        var ttl = await connection.GetDatabase().KeyTimeToLiveAsync("clima:asuncion");
        ttl.ShouldNotBeNull();
        ttl.Value.ShouldBeGreaterThan(TimeSpan.FromMinutes(9));
        ttl.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task HealthReady_ComRedis_DevolveHealthy()
    {
        await using var app = CreateAppWithRedis();

        using var response = await app.CreateClient().GetAsync("/health/ready", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("Healthy");
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateAppWithRedis()
    {
        return factory.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Redis", redis.Container.GetConnectionString()));
    }
}
```

- [ ] **Passo 10: rodar**

Run: `dotnet test --project tests/Climato.Provider.IntegrationTests` (Docker em execução)
Expected: 7 + 3 + 3 + 2 + 2 = 17 testes, 0 falhas, em menos de ~1 min após o pull das imagens. Trocar `ShouldBe(3)` por `ShouldBe(1)` no teste de 500, ver falhar, desfazer.

- [ ] **Passo 11: `/revisar-padrao` e commit**

```bash
git add Climato.slnx tests/Climato.Provider.IntegrationTests
git commit -m "Adiciona testes de integração do Provider com WireMock e Redis" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---
### Tarefa I2: Gateway — integração (Postgres + WireMock)

**Files:**
- Create: `tests/Climato.Gateway.IntegrationTests/Climato.Gateway.IntegrationTests.csproj`, `Infrastructure/GatewayApiFactory.cs`, `Infrastructure/GatewayTestExtensions.cs`, `Infrastructure/TestTokens.cs`, `Api/{Auth,Token,CityOwnership,Favorite,Weather,ForecastAndLocation,Health}EndpointTests.cs`
- Modify: `Climato.slnx`

**Interfaces:**
- Produces: `GatewayApiFactory` (assembly fixture; `Provider` = `WireMockServer`; constantes `JwtSecretKey`, `JwtIssuer`, `JwtAudience`); `GatewayTestExtensions` (`NewEmail()`, `NewCityName()`, `CreateAuthenticatedClientAsync`, `RegisterAsync`, `StubWeather`, `StubStatus`, `StubJson`, `SearchCityAsync`, `GetCitiesAsync`, `CountRequests`); `TestTokens.Create`.

- [ ] **Passo 1: `.csproj`** e linha no `Climato.slnx`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="WireMock.Net" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Gateway\Climato.Gateway.Api\Climato.Gateway.Api.csproj" />
  </ItemGroup>

</Project>
```

```xml
    <Project Path="tests/Climato.Gateway.IntegrationTests/Climato.Gateway.IntegrationTests.csproj" />
```

- [ ] **Passo 2: `Infrastructure/GatewayApiFactory.cs`**

Uma instância para o assembly inteiro. O `InitializeAsync` sobe o Postgres **e o host** (o startup aplica as migrations no ambiente `Testing`) antes de qualquer teste, para que hosts derivados (`WithWebHostBuilder`) encontrem o banco já migrado.

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Testcontainers.PostgreSql;
using WireMock.Server;

[assembly: AssemblyFixture(typeof(Climato.Gateway.IntegrationTests.Infrastructure.GatewayApiFactory))]

namespace Climato.Gateway.IntegrationTests.Infrastructure;

public sealed class GatewayApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecretKey = "chave-jwt-de-teste-do-gateway-com-mais-de-32-caracteres-0123456789";
    public const string JwtIssuer = "climato-gateway";
    public const string JwtAudience = "climato-web";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WireMockServer Provider { get; } = WireMockServer.Start();

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        StartServer();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Climato", postgres.GetConnectionString());
        builder.UseSetting("Provider:BaseUrl", $"{Provider.Url}/");
        builder.UseSetting("Jwt:SecretKey", JwtSecretKey);
        builder.ConfigureServices(services =>
            services.ConfigureAll<HttpStandardResilienceOptions>(options => options.Retry.Delay = TimeSpan.Zero));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Provider.Stop();
        await postgres.DisposeAsync();
    }
}
```

- [ ] **Passo 3: `Infrastructure/GatewayTestExtensions.cs`**

O WireMock é compartilhado pelo assembly e os testes rodam em paralelo: todo stub usa nome de cidade/consulta único (`NewCityName()`).

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.Application.Model.Weathers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Climato.Gateway.IntegrationTests.Infrastructure;

public static class GatewayTestExtensions
{
    public const string Password = "senha-de-teste-123";

    public static string NewEmail()
    {
        return $"usuario-{Guid.NewGuid():N}@teste.dev";
    }

    public static string NewCityName()
    {
        return $"Cidade{Guid.NewGuid():N}";
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this GatewayApiFactory factory, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient();
        await client.AuthenticateAsync(cancellationToken);
        return client;
    }

    public static async Task<AuthDto> AuthenticateAsync(this HttpClient client, CancellationToken cancellationToken)
    {
        var auth = await client.RegisterAsync(NewEmail(), cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return auth;
    }

    public static async Task<AuthDto> RegisterAsync(this HttpClient client, string email, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterPostDto { Email = email, Name = "Usuário de Teste", Password = Password },
            cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthDto>(cancellationToken);
        return auth.ShouldNotBeNull();
    }

    public static void StubWeather(this WireMockServer provider, string cityName)
    {
        provider.StubJson("/weathers", "cityName", cityName, Weather(cityName));
    }

    public static void StubJson<T>(this WireMockServer provider, string path, string paramName, string paramValue, T body)
    {
        provider.Given(Request.Create().WithPath(path).WithParam(paramName, paramValue).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(body, JsonSerializerOptions.Web)));
    }

    public static void StubStatus(this WireMockServer provider, string path, string paramName, string paramValue, int statusCode)
    {
        provider.Given(Request.Create().WithPath(path).WithParam(paramName, paramValue).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/problem+json")
                .WithBody("""{ "title": "erro do Provider" }"""));
    }

    public static int CountRequests(this WireMockServer provider, string path, string paramName, string paramValue)
    {
        return provider.FindLogEntries(Request.Create().WithPath(path).WithParam(paramName, paramValue)).Count;
    }

    public static async Task<CityDto> SearchCityAsync(this HttpClient client, WireMockServer provider, CancellationToken cancellationToken)
    {
        var cityName = NewCityName();
        provider.StubWeather(cityName);

        using var response = await client.GetAsync($"/weathers/{cityName}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cities = await client.GetCitiesAsync("/cities", cancellationToken);
        return cities.Single(city => city.Name == cityName);
    }

    public static async Task<List<CityDto>> GetCitiesAsync(this HttpClient client, string path, CancellationToken cancellationToken)
    {
        var cities = await client.GetFromJsonAsync<List<CityDto>>(path, cancellationToken);
        return cities.ShouldNotBeNull();
    }

    private static WeatherDto Weather(string cityName)
    {
        return new WeatherDto
        {
            City = cityName,
            Country = "PE",
            Description = "nublado",
            Icon = "04d",
            Temperature = 18,
            FeelsLike = 18,
            TempMin = 16,
            TempMax = 20,
            Humidity = 80,
            Pressure = 1012,
            WindSpeedKmh = 10,
            WindDirection = 180,
            Sunrise = new DateTimeOffset(2026, 10, 26, 5, 45, 0, TimeSpan.FromHours(-5)),
            Sunset = new DateTimeOffset(2026, 10, 26, 18, 10, 0, TimeSpan.FromHours(-5))
        };
    }
}
```

- [ ] **Passo 4: `Infrastructure/TestTokens.cs`**

```csharp
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Climato.Gateway.IntegrationTests.Infrastructure;

public static class TestTokens
{
    public static string Create(string secretKey, Guid userId, DateTime notBefore, DateTime expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = GatewayApiFactory.JwtIssuer,
            Audience = GatewayApiFactory.JwtAudience,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = userId.ToString() },
            IssuedAt = notBefore,
            NotBefore = notBefore,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
```

- [ ] **Passo 5: `Api/AuthEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Gateway.Application.Model.Auth;
using Climato.Gateway.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.IntegrationTests.Api;

public class AuthEndpointTests(GatewayApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_DadosValidos_Devolve201ComTokenEUsuario()
    {
        var email = GatewayTestExtensions.NewEmail();

        var auth = await client.RegisterAsync(email, cancellationToken);

        auth.Token.ShouldNotBeNullOrWhiteSpace();
        auth.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        auth.User.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Register_EmailJaCadastradoEmOutraCaixa_Devolve409()
    {
        var email = GatewayTestExtensions.NewEmail();
        await client.RegisterAsync(email, cancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterPostDto { Email = email.ToUpperInvariant(), Name = "Outro", Password = GatewayTestExtensions.Password },
            cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("E-mail já cadastrado.");
    }

    [Fact]
    public async Task Register_SenhaCurta_Devolve400ComErrosDeValidacao()
    {
        using var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterPostDto { Email = GatewayTestExtensions.NewEmail(), Name = "Ana", Password = "curta" },
            cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("Dados inválidos.");
        problem.Errors["Password"].ShouldContain("A senha deve ter entre 8 e 128 caracteres.");
    }

    [Fact]
    public async Task Login_CredenciaisValidasComEmailEmOutraCaixa_Devolve200ComToken()
    {
        var email = GatewayTestExtensions.NewEmail();
        await client.RegisterAsync(email, cancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginPostDto { Email = email.ToUpperInvariant(), Password = GatewayTestExtensions.Password },
            cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthDto>(cancellationToken);
        auth!.Token.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_SenhaErrada_Devolve401()
    {
        var email = GatewayTestExtensions.NewEmail();
        await client.RegisterAsync(email, cancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginPostDto { Email = email, Password = "senha-errada-123" },
            cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("E-mail ou senha inválidos.");
    }
}
```

- [ ] **Passo 6: `Api/TokenEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Climato.Gateway.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.IntegrationTests.Api;

public class TokenEndpointTests(GatewayApiFactory factory)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetCities_SemToken_Devolve401()
    {
        using var response = await factory.CreateClient().GetAsync("/cities", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCities_TokenDaChaveCorreta_Devolve200()
    {
        var userId = await RegisterUserIdAsync();
        var token = TestTokens.Create(GatewayApiFactory.JwtSecretKey, userId, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10));

        using var response = await GetCitiesWithTokenAsync(token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCities_TokenAssinadoComOutraChave_Devolve401()
    {
        var userId = await RegisterUserIdAsync();
        var token = TestTokens.Create(new string('x', 64), userId, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10));

        using var response = await GetCitiesWithTokenAsync(token);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCities_TokenExpirado_Devolve401()
    {
        var userId = await RegisterUserIdAsync();
        var token = TestTokens.Create(GatewayApiFactory.JwtSecretKey, userId, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));

        using var response = await GetCitiesWithTokenAsync(token);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeather_UsuarioApagado_Devolve401SessaoInvalida()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        using var deleteResponse = await client.DeleteAsync("/users/me", cancellationToken);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubWeather(cityName);

        using var response = await client.GetAsync($"/weathers/{cityName}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("Sessão inválida.");
    }

    private async Task<Guid> RegisterUserIdAsync()
    {
        var auth = await factory.CreateClient().RegisterAsync(GatewayTestExtensions.NewEmail(), cancellationToken);
        return auth.User.Id;
    }

    private async Task<HttpResponseMessage> GetCitiesWithTokenAsync(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/cities", cancellationToken);
    }
}
```

- [ ] **Passo 7: `Api/CityOwnershipEndpointTests.cs`**

```csharp
using System.Net;
using Climato.Gateway.IntegrationTests.Infrastructure;

namespace Climato.Gateway.IntegrationTests.Api;

public class CityOwnershipEndpointTests(GatewayApiFactory factory)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ToggleFavorite_CidadeDeOutroUsuario_Devolve404EMantemACidade()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var city = await owner.SearchCityAsync(factory.Provider, cancellationToken);
        var other = await factory.CreateAuthenticatedClientAsync(cancellationToken);

        using var response = await other.PatchAsync($"/cities/{city.Id}/favorite", null, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var ownerCities = await owner.GetCitiesAsync("/cities", cancellationToken);
        ownerCities.Single(c => c.Id == city.Id).IsFavorite.ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_CidadeDeOutroUsuario_Devolve404EMantemACidade()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var city = await owner.SearchCityAsync(factory.Provider, cancellationToken);
        var other = await factory.CreateAuthenticatedClientAsync(cancellationToken);

        using var response = await other.DeleteAsync($"/cities/{city.Id}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var ownerCities = await owner.GetCitiesAsync("/cities", cancellationToken);
        ownerCities.ShouldContain(c => c.Id == city.Id);
    }

    [Fact]
    public async Task GetAll_OutroUsuario_NaoVeAsCidadesNemAsFavoritas()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var city = await owner.SearchCityAsync(factory.Provider, cancellationToken);
        using var favoriteResponse = await owner.PatchAsync($"/cities/{city.Id}/favorite", null, cancellationToken);
        favoriteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var other = await factory.CreateAuthenticatedClientAsync(cancellationToken);

        var otherCities = await other.GetCitiesAsync("/cities", cancellationToken);
        var otherFavorites = await other.GetCitiesAsync("/cities/favorites", cancellationToken);

        otherCities.ShouldBeEmpty();
        otherFavorites.ShouldBeEmpty();
    }
}
```

- [ ] **Passo 8: `Api/FavoriteEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Gateway.Application.Model.Cities;
using Climato.Gateway.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.IntegrationTests.Api;

public class FavoriteEndpointTests(GatewayApiFactory factory)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ToggleFavorite_DuasVezes_AlternaOEstadoEAListaDeFavoritas()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var city = await client.SearchCityAsync(factory.Provider, cancellationToken);

        var favorited = await ToggleAsync(client, city.Id);
        var favoritesAfterFirst = await client.GetCitiesAsync("/cities/favorites", cancellationToken);
        var unfavorited = await ToggleAsync(client, city.Id);
        var favoritesAfterSecond = await client.GetCitiesAsync("/cities/favorites", cancellationToken);

        favorited.IsFavorite.ShouldBeTrue();
        favoritesAfterFirst.ShouldContain(c => c.Id == city.Id);
        unfavorited.IsFavorite.ShouldBeFalse();
        favoritesAfterSecond.ShouldNotContain(c => c.Id == city.Id);
    }

    [Fact]
    public async Task ToggleFavorite_CidadeInexistente_Devolve404()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);

        using var response = await client.PatchAsync($"/cities/{Guid.NewGuid()}/favorite", null, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_CidadeFavorita_Devolve409EMantemACidade()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var city = await client.SearchCityAsync(factory.Provider, cancellationToken);
        await ToggleAsync(client, city.Id);

        using var response = await client.DeleteAsync($"/cities/{city.Id}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Detail.ShouldBe("Não é possível remover uma cidade favorita.");
        (await client.GetCitiesAsync("/cities", cancellationToken)).ShouldContain(c => c.Id == city.Id);
    }

    [Fact]
    public async Task Delete_CidadeNaoFavorita_Devolve204ESomeDaLista()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var city = await client.SearchCityAsync(factory.Provider, cancellationToken);

        using var response = await client.DeleteAsync($"/cities/{city.Id}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetCitiesAsync("/cities", cancellationToken)).ShouldNotContain(c => c.Id == city.Id);
    }

    private async Task<CityDto> ToggleAsync(HttpClient client, Guid cityId)
    {
        using var response = await client.PatchAsync($"/cities/{cityId}/favorite", null, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var city = await response.Content.ReadFromJsonAsync<CityDto>(cancellationToken);
        return city.ShouldNotBeNull();
    }
}
```

- [ ] **Passo 9: `Api/WeatherEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.IntegrationTests.Api;

public class WeatherEndpointTests(GatewayApiFactory factory)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetWeather_ProviderDevolveClima_Devolve200ESalvaNoHistorico()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubWeather(cityName);

        using var response = await client.GetAsync($"/weathers/{cityName}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var weather = await response.Content.ReadFromJsonAsync<WeatherDto>(cancellationToken);
        weather!.City.ShouldBe(cityName);
        var city = (await client.GetCitiesAsync("/cities", cancellationToken)).ShouldHaveSingleItem();
        city.Name.ShouldBe(cityName);
        city.Country.ShouldBe("PE");
        city.IsFavorite.ShouldBeFalse();
    }

    [Fact]
    public async Task GetWeather_MesmaCidadeDuasVezes_SalvaUmaVez()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubWeather(cityName);

        using var first = await client.GetAsync($"/weathers/{cityName}", cancellationToken);
        using var second = await client.GetAsync($"/weathers/{cityName}", cancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetCitiesAsync("/cities", cancellationToken)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetWeather_ProviderDevolve404_Devolve404ENaoSalva()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubStatus("/weathers", "cityName", cityName, 404);

        using var response = await client.GetAsync($"/weathers/{cityName}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("Cidade não encontrada.");
        (await client.GetCitiesAsync("/cities", cancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetWeather_ProviderDevolve500_Devolve503SemRetentar()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubStatus("/weathers", "cityName", cityName, 500);

        using var response = await client.GetAsync($"/weathers/{cityName}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        problem!.Title.ShouldBe("Serviço de clima indisponível. Tente novamente em instantes.");
        factory.Provider.CountRequests("/weathers", "cityName", cityName).ShouldBe(1);
    }

    [Fact]
    public async Task GetWeather_ProviderFora_Devolve503()
    {
        await using var providerDown = factory.WithWebHostBuilder(builder => builder.UseSetting("Provider:BaseUrl", "http://127.0.0.1:1/"));
        var client = providerDown.CreateClient();
        await client.AuthenticateAsync(cancellationToken);

        using var response = await client.GetAsync($"/weathers/{GatewayTestExtensions.NewCityName()}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
```

- [ ] **Passo 10: `Api/ForecastAndLocationEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using Climato.Gateway.Application.Model.Locations;
using Climato.Gateway.Application.Model.Weathers;
using Climato.Gateway.IntegrationTests.Infrastructure;

namespace Climato.Gateway.IntegrationTests.Api;

public class ForecastAndLocationEndpointTests(GatewayApiFactory factory)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetForecast_ProviderDevolvePrevisao_Repassa200()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubJson("/forecasts", "cityName", cityName, new ForecastDto
        {
            City = cityName,
            Country = "PE",
            Days =
            [
                new ForecastDayDto
                {
                    Date = new DateOnly(2026, 10, 26), Description = "nublado", Icon = "04d",
                    TempMin = 16, TempMax = 20, PrecipitationProbability = 30, WindSpeedKmh = 12.6
                }
            ]
        });

        var forecast = await client.GetFromJsonAsync<ForecastDto>($"/forecasts/{cityName}", cancellationToken);

        forecast.ShouldNotBeNull();
        forecast.City.ShouldBe(cityName);
        forecast.Days.ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 10, 26));
    }

    [Theory]
    [InlineData(404, HttpStatusCode.NotFound)]
    [InlineData(500, HttpStatusCode.ServiceUnavailable)]
    public async Task GetForecast_ProviderFalha_MapeiaOStatus(int providerStatus, HttpStatusCode expected)
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var cityName = GatewayTestExtensions.NewCityName();
        factory.Provider.StubStatus("/forecasts", "cityName", cityName, providerStatus);

        using var response = await client.GetAsync($"/forecasts/{cityName}", cancellationToken);

        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task SearchLocations_ProviderDevolveLista_RepassaComQueryAparada()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);
        var query = GatewayTestExtensions.NewCityName();
        factory.Provider.StubJson("/locations", "query", query, new[] { new LocationDto { Name = "Lima", Country = "PE" } });

        var locations = await client.GetFromJsonAsync<List<LocationDto>>($"/locations?query=%20{query}%20", cancellationToken);

        locations.ShouldNotBeNull();
        locations.ShouldHaveSingleItem().Name.ShouldBe("Lima");
    }

    [Fact]
    public async Task SearchLocations_QueryVazia_Devolve400()
    {
        var client = await factory.CreateAuthenticatedClientAsync(cancellationToken);

        using var response = await client.GetAsync("/locations?query=", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Passo 11: `Api/HealthEndpointTests.cs`**

```csharp
using System.Net;
using Climato.Gateway.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Climato.Gateway.IntegrationTests.Api;

public class HealthEndpointTests(GatewayApiFactory factory)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Live_SemToken_DevolveHealthy()
    {
        using var response = await factory.CreateClient().GetAsync("/health/live", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_PostgresEProviderSaudaveis_DevolveHealthy()
    {
        factory.Provider.Given(Request.Create().WithPath("/health/ready").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("Healthy"));

        using var response = await factory.CreateClient().GetAsync("/health/ready", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe("Healthy");
    }
}
```

- [ ] **Passo 12: rodar**

Run: `dotnet test --project tests/Climato.Gateway.IntegrationTests` (Docker em execução)
Expected: 5 + 5 + 3 + 4 + 5 + 5 + 2 = 29 testes, 0 falhas. Trocar `ShouldBe(1)` por `ShouldBe(2)` no teste de 500, ver falhar, desfazer.

- [ ] **Passo 13: `/revisar-padrao` e commit**

```bash
git add Climato.slnx tests/Climato.Gateway.IntegrationTests
git commit -m "Adiciona testes de integração do Gateway com Postgres e WireMock" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---
