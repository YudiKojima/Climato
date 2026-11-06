using System.Diagnostics;

namespace Climato.Gateway.Api.Observability;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;

        if (activity is not null)
        {
            var traceId = activity.TraceId.ToHexString();
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = traceId;
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}
