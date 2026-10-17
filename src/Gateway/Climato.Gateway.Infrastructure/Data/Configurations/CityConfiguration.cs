using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Climato.Gateway.Infrastructure.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("city");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("userid");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Country).HasColumnName("country").HasMaxLength(255).IsRequired();
        builder.Property(e => e.IsFavorite).HasColumnName("isfavorite");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("createdat")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_city_user");

        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_city_userid");
    }
}
