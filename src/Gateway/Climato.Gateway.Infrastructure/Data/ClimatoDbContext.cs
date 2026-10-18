using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Climato.Gateway.Infrastructure.Data;

public class ClimatoDbContext : DbContext
{
    public ClimatoDbContext(DbContextOptions<ClimatoDbContext> options) : base(options) { }

    public DbSet<City> Cities => Set<City>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClimatoDbContext).Assembly);
    }
}
