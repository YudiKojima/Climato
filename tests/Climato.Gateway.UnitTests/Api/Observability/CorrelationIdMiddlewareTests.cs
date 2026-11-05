using System.Diagnostics;
using Climato.Gateway.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Climato.Gateway.UnitTests.Api.Observability;

public class CorrelationIdMiddlewareTests
{
    private readonly StartingResponseFeature responseFeature = new();
    private readonly DefaultHttpContext httpContext = new();
    private bool nextCalled;

    public CorrelationIdMiddlewareTests()
    {
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);
    }

    [Fact]
    public async Task InvokeAsync_ComActivity_GravaOTraceIdNoHeaderAoIniciarAResposta()
    {
        using var activity = new Activity("teste").Start();
        var middleware = new CorrelationIdMiddleware(Next);

        await middleware.InvokeAsync(httpContext);
        await responseFeature.StartAsync();

        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().ShouldBe(activity.TraceId.ToHexString());
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_SemActivity_NaoGravaHeaderEChamaONext()
    {
        Activity.Current = null;
        var middleware = new CorrelationIdMiddleware(Next);

        await middleware.InvokeAsync(httpContext);
        await responseFeature.StartAsync();

        httpContext.Response.Headers.ContainsKey(CorrelationIdMiddleware.HeaderName).ShouldBeFalse();
        nextCalled.ShouldBeTrue();
    }

    private Task Next(HttpContext context)
    {
        nextCalled = true;
        return Task.CompletedTask;
    }

    private sealed class StartingResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> callbacks = [];

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            callbacks.Add((callback, state));
        }

        public async Task StartAsync()
        {
            foreach (var (callback, state) in callbacks)
            {
                await callback(state);
            }
        }
    }
}
