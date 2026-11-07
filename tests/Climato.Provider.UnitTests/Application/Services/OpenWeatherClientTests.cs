using System.Globalization;
using System.Net;
using Climato.Provider.Application.Options;
using Climato.Provider.Application.Services;
using Microsoft.Extensions.Options;

namespace Climato.Provider.UnitTests.Application.Services;

public class OpenWeatherClientTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetCurrentAsync_CulturaPtBr_EnviaCoordenadasComPonto()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        try
        {
            await client.GetCurrentAsync(-23.55, -46.63, cancellationToken);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.Query.ShouldContain("lat=-23.55&lon=-46.63");
        handler.RequestUri.AbsolutePath.ShouldBe("/data/2.5/weather");
    }

    [Fact]
    public async Task GetForecastAsync_Coordenadas_ConsultaAPrevisaoPorLatLon()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);

        await client.GetForecastAsync(35.68, 139.76, cancellationToken);

        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.AbsolutePath.ShouldBe("/data/2.5/forecast");
        handler.RequestUri.Query.ShouldContain("lat=35.68&lon=139.76");
    }

    private static OpenWeatherClient CreateClient(HttpMessageHandler handler)
    {
        var options = Options.Create(new OpenWeatherOptions { BaseUrl = "https://openweather.test/", ApiKey = "chave-de-teste" });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.Value.BaseUrl) };
        return new OpenWeatherClient(httpClient, options);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
