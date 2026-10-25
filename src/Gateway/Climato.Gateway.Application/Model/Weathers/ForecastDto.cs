namespace Climato.Gateway.Application.Model.Weathers;

public sealed class ForecastDto
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required IReadOnlyList<ForecastDayDto> Days { get; init; }
}
