namespace Climato.Gateway.Application.Model.Weathers;

public sealed class ForecastDayDto
{
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required double TempMin { get; init; }
    public required double TempMax { get; init; }
    public required int PrecipitationProbability { get; init; }
    public required double WindSpeedKmh { get; init; }
}
