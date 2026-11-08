using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Climato.Gateway.Api.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Climato.Gateway.UnitTests.Api.RateLimiting;

public class RateLimitPoliciesTests
{
    private const string DemoEmail = "demo@climato.dev";

    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public void PartitionKey_UsuarioAutenticado_UsaOSub()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "abc")], "Bearer"))
        };

        RateLimitPolicies.PartitionKey(context, DemoEmail).ShouldBe("user:abc");
    }

    [Fact]
    public void PartitionKey_UsuarioDemo_SeparaPorIp()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, "demo-id"), new Claim(JwtRegisteredClaimNames.Email, DemoEmail)],
                "Bearer"))
        };
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        RateLimitPolicies.PartitionKey(context, " Demo@Climato.dev").ShouldBe("user:demo-id:ip:203.0.113.7");
    }

    [Fact]
    public void PartitionKey_Anonimo_UsaOIp()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        RateLimitPolicies.PartitionKey(context, DemoEmail).ShouldBe("ip:203.0.113.7");
    }

    [Fact]
    public async Task OnRejectedAsync_Devolve429ComRetryAfterEProblemDetailsEmPortugues()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        var rejected = new OnRejectedContext { HttpContext = context, Lease = new RejectedLease(TimeSpan.FromSeconds(42)) };

        await RateLimitPolicies.OnRejectedAsync(rejected, cancellationToken);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.ToString().ShouldBe("42");
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: cancellationToken);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Muitas requisições. Tente novamente em instantes.");
    }

    private sealed class RejectedLease(TimeSpan retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = metadataName == MetadataName.RetryAfter.Name ? retryAfter : null;
            return metadata != null;
        }
    }
}
