using Climato.Gateway.Application.Model.Weathers;

namespace Climato.Gateway.UnitTests;

internal static class TestData
{
    public static WeatherDto Weather(string city, string country = "PE")
    {
        return new WeatherDto
        {
            City = city,
            Country = country,
            Description = "nublado",
            Icon = "04d",
            Temperature = 18,
            FeelsLike = 18,
            TempMin = 16,
            TempMax = 20,
            Humidity = 80,
            Pressure = 1012,
            WindSpeedKmh = 10,
            WindDirection = 180,
            Sunrise = new DateTimeOffset(2026, 10, 26, 5, 45, 0, TimeSpan.FromHours(-5)),
            Sunset = new DateTimeOffset(2026, 10, 26, 18, 10, 0, TimeSpan.FromHours(-5))
        };
    }
}
