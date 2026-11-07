namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherLocationResponse(
    string Name,
    IReadOnlyDictionary<string, string>? LocalNames,
    string Country,
    string? State,
    double Lat,
    double Lon)
{
    private const string PortugueseLanguageCode = "pt";

    public string PortugueseName => LocalNames?.GetValueOrDefault(PortugueseLanguageCode) ?? Name;
}
