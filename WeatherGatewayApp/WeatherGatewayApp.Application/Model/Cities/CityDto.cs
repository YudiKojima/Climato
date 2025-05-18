namespace WeatherGatewayApp.Application.Cities
{
    public class CityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
