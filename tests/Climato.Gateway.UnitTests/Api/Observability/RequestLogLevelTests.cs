using Climato.Gateway.Api.Observability;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace Climato.Gateway.UnitTests.Api.Observability;

public class RequestLogLevelTests
{
    [Fact]
    public void Get_HealthCheckComFalha_DevolveVerbose()
    {
        var context = CreateContext("/health/ready", StatusCodes.Status503ServiceUnavailable);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Verbose);
    }

    [Fact]
    public void Get_ComExcecao_DevolveError()
    {
        var context = CreateContext("/cities", StatusCodes.Status200OK);

        var level = RequestLogLevel.Get(context, 1, new InvalidOperationException("falha"));

        level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void Get_Status503_DevolveError()
    {
        var context = CreateContext("/weathers/londres", StatusCodes.Status503ServiceUnavailable);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Error);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status404NotFound)]
    public void Get_StatusAbaixoDe500_DevolveInformation(int statusCode)
    {
        var context = CreateContext("/cities", statusCode);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Information);
    }

    private static DefaultHttpContext CreateContext(string path, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;
        return context;
    }
}
