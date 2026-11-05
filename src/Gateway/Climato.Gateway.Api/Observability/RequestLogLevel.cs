using Serilog.Events;

namespace Climato.Gateway.Api.Observability;

public static class RequestLogLevel
{
    public static LogEventLevel Get(HttpContext context, double elapsedMilliseconds, Exception? exception)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Verbose;
        }

        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return LogEventLevel.Information;
    }
}
