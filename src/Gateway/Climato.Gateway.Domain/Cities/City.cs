using Climato.Gateway.Domain.Exceptions;

namespace Climato.Gateway.Domain.Cities;

public class City
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
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

    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    public void EnsureCanBeDeleted()
    {
        if (IsFavorite)
        {
            throw new DomainException("Não é possível remover uma cidade favorita.");
        }
    }
}
