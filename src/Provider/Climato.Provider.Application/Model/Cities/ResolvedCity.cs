namespace Climato.Provider.Application.Model.Cities;

public sealed record ResolvedCity(string Name, string OriginalName, string Country, double Lat, double Lon);
