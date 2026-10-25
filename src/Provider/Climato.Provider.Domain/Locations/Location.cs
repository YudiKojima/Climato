namespace Climato.Provider.Domain.Locations;

public sealed class Location
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public required string Country { get; init; }
}
