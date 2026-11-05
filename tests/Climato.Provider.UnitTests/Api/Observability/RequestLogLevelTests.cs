using Climato.Provider.Api.Observability;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace Climato.Provider.UnitTests.Api.Observability;

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
        var context = CreateContext("/weathers", StatusCodes.Status200OK);

        var level = RequestLogLevel.Get(context, 1, new InvalidOperationException("falha"));

        level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void Get_Status500_DevolveError()
    {
        var context = CreateContext("/weathers", StatusCodes.Status500InternalServerError);

        var level = RequestLogLevel.Get(context, 1, null);

        level.ShouldBe(LogEventLevel.Error);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status499ClientClosedRequest)]
    public void Get_StatusAbaixoDe500_DevolveInformation(int statusCode)
    {
        var context = CreateContext("/weathers", statusCode);

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
