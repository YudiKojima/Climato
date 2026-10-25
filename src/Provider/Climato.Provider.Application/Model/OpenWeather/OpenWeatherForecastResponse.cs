namespace Climato.Provider.Application.Model.OpenWeather;

public sealed record OpenWeatherForecastResponse(IReadOnlyList<OpenWeatherForecastItem> List, OpenWeatherForecastCity City);

public sealed record OpenWeatherForecastItem(
    long Dt,
    OpenWeatherForecastMain Main,
    IReadOnlyList<OpenWeatherCondition> Weather,
    OpenWeatherForecastWind Wind,
    double Pop);

public sealed record OpenWeatherForecastMain(double TempMin, double TempMax);

public sealed record OpenWeatherForecastWind(double Speed);

public sealed record OpenWeatherForecastCity(string Name, string Country, int Timezone);
