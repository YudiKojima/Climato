namespace WeatherGatewayApp.Domain.Weathers
{
    public class Weather
    {
        public string City { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
    }

}
