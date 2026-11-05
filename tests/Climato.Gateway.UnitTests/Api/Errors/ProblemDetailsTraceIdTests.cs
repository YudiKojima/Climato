using System.Diagnostics;
using Climato.Gateway.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Climato.Gateway.UnitTests.Api.Errors;

public class ProblemDetailsTraceIdTests
{
    [Fact]
    public void Apply_ComActivity_GravaOTraceIdPuro()
    {
        using var activity = new Activity("teste").Start();
        var context = CreateContext();

        ProblemDetailsTraceId.Apply(context);

        context.ProblemDetails.Extensions["traceId"].ShouldBe(activity.TraceId.ToHexString());
    }

    [Fact]
    public void Apply_SemActivity_GravaOTraceIdentifier()
    {
        Activity.Current = null;
        var context = CreateContext();
        context.HttpContext.TraceIdentifier = "0HN-teste";

        ProblemDetailsTraceId.Apply(context);

        context.ProblemDetails.Extensions["traceId"].ShouldBe("0HN-teste");
    }

    private static ProblemDetailsContext CreateContext() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ProblemDetails = new ProblemDetails()
    };
}
