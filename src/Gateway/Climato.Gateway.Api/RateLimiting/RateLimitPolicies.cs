using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Climato.Gateway.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Climato.Gateway.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Api = "api";

    public static string PartitionKey(HttpContext context, string demoEmail)
    {
        var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId == null)
        {
            return IpPartitionKey(context);
        }

        var email = context.User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var isDemo = email != null && User.NormalizeEmail(email) == User.NormalizeEmail(demoEmail);

        return isDemo ? $"user:{userId}:{IpPartitionKey(context)}" : $"user:{userId}";
    }

    public static string IpPartitionKey(HttpContext context)
    {
        return $"ip:{context.Connection.RemoteIpAddress}";
    }

    public static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas requisições. Tente novamente em instantes."
            }
        });
    }
}
