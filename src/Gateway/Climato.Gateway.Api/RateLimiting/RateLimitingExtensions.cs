using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Climato.Gateway.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.OnRejected = RateLimitPolicies.OnRejectedAsync;
            limiter.AddPolicy(RateLimitPolicies.Auth, context =>
                FixedWindow(RateLimitPolicies.IpPartitionKey(context), Options(context).AuthPermitLimit, Options(context)));
            limiter.AddPolicy(RateLimitPolicies.Api, context =>
                FixedWindow(RateLimitPolicies.PartitionKey(context), Options(context).ApiPermitLimit, Options(context)));
        });

        return services;
    }

    private static RateLimitingOptions Options(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
    }

    private static RateLimitPartition<string> FixedWindow(string key, int permitLimit, RateLimitingOptions options)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0
        });
    }
}
