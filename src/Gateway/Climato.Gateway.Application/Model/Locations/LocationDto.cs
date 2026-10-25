namespace Climato.Gateway.Application.Model.Locations;

public sealed class LocationDto
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public required string Country { get; init; }
}
