using Climato.Provider.Application.Model.OpenWeather;

namespace Climato.Provider.UnitTests;

internal static class OpenWeatherFakes
{
    public static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0)
    {
        return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
    }

    public static OpenWeatherCurrentResponse Current(
        string name,
        TimeSpan offset,
        DateTimeOffset sunriseUtc,
        DateTimeOffset sunsetUtc,
        double windSpeed = 5.0)
    {
        return new OpenWeatherCurrentResponse(
            name,
            (int)offset.TotalSeconds,
            new OpenWeatherSys("BR", sunriseUtc.ToUnixTimeSeconds(), sunsetUtc.ToUnixTimeSeconds()),
            [new OpenWeatherCondition("céu limpo", "01d")],
            new OpenWeatherMain(25.4, 26.1, 22.0, 28.3, 60, 1013),
            new OpenWeatherWind(windSpeed, 180));
    }

    public static OpenWeatherForecastItem Item(
        DateTimeOffset utc,
        double tempMin = 20,
        double tempMax = 25,
        double pop = 0,
        double windSpeed = 1,
        string description = "nublado",
        string icon = "04d")
    {
        return new OpenWeatherForecastItem(
            utc.ToUnixTimeSeconds(),
            new OpenWeatherForecastMain(tempMin, tempMax),
            [new OpenWeatherCondition(description, icon)],
            new OpenWeatherForecastWind(windSpeed),
            pop);
    }

    public static OpenWeatherForecastResponse Forecast(TimeSpan offset, params OpenWeatherForecastItem[] items)
    {
        return new OpenWeatherForecastResponse(items, new OpenWeatherForecastCity("Cidade", "XX", (int)offset.TotalSeconds));
    }
}
