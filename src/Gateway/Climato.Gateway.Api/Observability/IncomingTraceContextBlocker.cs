using System.Diagnostics;

namespace Climato.Gateway.Api.Observability;

public sealed class IncomingTraceContextBlocker : DistributedContextPropagator
{
    private readonly DistributedContextPropagator inner = CreateDefaultPropagator();

    public override IReadOnlyCollection<string> Fields => inner.Fields;

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
    {
        inner.Inject(activity, carrier, setter);
    }

    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
    {
        traceId = null;
        traceState = null;
    }

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
    {
        return null;
    }
}
