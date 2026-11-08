using System.Diagnostics;
using Climato.Gateway.Api.Observability;

namespace Climato.Gateway.UnitTests.Api.Observability;

public class IncomingTraceContextBlockerTests
{
    private readonly IncomingTraceContextBlocker propagator = new();

    [Fact]
    public void ExtractTraceIdAndState_IgnoraOTraceparentRecebido()
    {
        var headers = new Dictionary<string, string> { ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" };

        propagator.ExtractTraceIdAndState(headers, Getter, out var traceId, out var traceState);

        traceId.ShouldBeNull();
        traceState.ShouldBeNull();
    }

    [Fact]
    public void ExtractBaggage_IgnoraOBaggageRecebido()
    {
        var headers = new Dictionary<string, string> { ["baggage"] = "usuario=intruso" };

        propagator.ExtractBaggage(headers, Getter).ShouldBeNull();
    }

    [Fact]
    public void Inject_PropagaAAtividadeAtual()
    {
        using var activity = new Activity("teste").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, string>();

        propagator.Inject(activity, headers, (carrier, name, value) => ((Dictionary<string, string>)carrier!)[name] = value);

        headers["traceparent"].ShouldContain(activity.TraceId.ToHexString());
    }

    private static void Getter(object? carrier, string name, out string? value, out IEnumerable<string>? values)
    {
        ((Dictionary<string, string>)carrier!).TryGetValue(name, out value);
        values = null;
    }
}
