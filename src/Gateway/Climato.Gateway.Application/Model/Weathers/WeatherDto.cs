namespace Climato.Gateway.Application.Model.Weathers;

public sealed class WeatherDto
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required double Temperature { get; init; }
    public required double FeelsLike { get; init; }
    public required double TempMin { get; init; }
    public required double TempMax { get; init; }
    public required int Humidity { get; init; }
    public required int Pressure { get; init; }
    public required double WindSpeedKmh { get; init; }
    public required int WindDirection { get; init; }
    public required DateTimeOffset Sunrise { get; init; }
    public required DateTimeOffset Sunset { get; init; }
    public string? ImageUrl { get; init; }
}
