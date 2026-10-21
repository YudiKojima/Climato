namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherCurrentResponse(
    string Name,
    int Timezone,
    OpenWeatherSys Sys,
    IReadOnlyList<OpenWeatherCondition> Weather,
    OpenWeatherMain Main,
    OpenWeatherWind Wind);

public sealed record OpenWeatherSys(string Country, long Sunrise, long Sunset);

public sealed record OpenWeatherCondition(string Description, string Icon);

public sealed record OpenWeatherMain(double Temp, double FeelsLike, double TempMin, double TempMax, int Humidity, int Pressure);

public sealed record OpenWeatherWind(double Speed, int Deg);
