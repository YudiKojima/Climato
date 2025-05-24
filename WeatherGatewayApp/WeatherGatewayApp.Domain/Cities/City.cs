using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherGatewayApp.Domain.Cities
{
    [Table("city")]
    public class City
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }

        public City() { }

        public City(string name, string country, Guid userId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Name = name ?? string.Empty;
            Country = country ?? string.Empty;
            IsFavorite = false;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
