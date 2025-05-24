using Microsoft.EntityFrameworkCore;
using WeatherGatewayApp.Domain.Cities;
using WeatherGatewayApp.Domain.Users;

namespace WeatherGatewayApp.Infrastructure
{
    public class ConnectionContext : DbContext
    {
        public DbSet<City> City { get; set; }
        public DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Server=localhost; Port=5432; Database=weather-app; User Id=postgres; Password=cs305;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>(entity =>
            {
                entity.ToTable("city");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("userid");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Country).HasColumnName("country");
                entity.Property(e => e.IsFavorite).HasColumnName("isfavorite");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamp with time zone")
                .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamp with time zone")
                .ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

                entity.HasIndex(e => e.Email).IsUnique();
            });
        }
    }
}
