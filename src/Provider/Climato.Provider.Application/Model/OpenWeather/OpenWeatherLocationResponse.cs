namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherLocationResponse(
    string Name,
    IReadOnlyDictionary<string, string>? LocalNames,
    string Country,
    string? State);
