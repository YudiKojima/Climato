using System.Net;
using Climato.Gateway.Infrastructure.Http;

namespace Climato.Gateway.UnitTests.Infrastructure.Http;

public class WeatherProviderClientTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(true, "/weathers?cityName=S%C3%A3o%20Paulo")]
    [InlineData(false, "/weathers?cityName=S%C3%A3o%20Paulo&image=false")]
    public async Task GetWeatherAsync_MontaAUrlConformeAImagem(bool includeImage, string expectedPathAndQuery)
    {
        var handler = new CapturingHandler();
        var client = new WeatherProviderClient(new HttpClient(handler) { BaseAddress = new Uri("http://provider/") });

        await client.GetWeatherAsync("São Paulo", includeImage, cancellationToken);

        handler.Request!.RequestUri!.PathAndQuery.ShouldBe(expectedPathAndQuery);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
