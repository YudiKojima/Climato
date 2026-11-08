using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int AuthPermitLimit { get; init; } = 10;

    [Range(1, int.MaxValue)]
    public int ApiPermitLimit { get; init; } = 120;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; init; } = 60;
}
