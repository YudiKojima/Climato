using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Climato.Gateway.Infrastructure.Http;

public sealed class ProviderHealthCheck : IHealthCheck
{
    public const string HttpClientName = "ProviderHealth";

    private readonly IHttpClientFactory httpClientFactory;

    public ProviderHealthCheck(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync("health/ready", cancellationToken);

        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"Provider respondeu {(int)response.StatusCode}.");
    }
}
