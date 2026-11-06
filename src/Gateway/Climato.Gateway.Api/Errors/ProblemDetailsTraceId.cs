using System.Diagnostics;

namespace Climato.Gateway.Api.Errors;

public static class ProblemDetailsTraceId
{
    public static void Apply(ProblemDetailsContext context)
    {
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;
    }
}
