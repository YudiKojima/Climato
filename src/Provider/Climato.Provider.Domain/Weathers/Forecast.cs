namespace Climato.Provider.Domain.Weathers;

public sealed class Forecast
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required IReadOnlyList<ForecastDay> Days { get; init; }
}
