using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration
    : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LicensePlate)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.MaxWeightKg)
            .HasPrecision(12, 2);

        builder.Property(x => x.MaxVolumeM3)
            .HasPrecision(12, 3);

        builder.Property(x => x.LengthM)
            .HasPrecision(8, 2);

        builder.Property(x => x.WidthM)
            .HasPrecision(8, 2);

        builder.Property(x => x.HeightM)
            .HasPrecision(8, 2);

        builder.HasIndex(x => x.LicensePlate)
            .IsUnique();

        builder.HasOne<TransporterProfile>()
            .WithMany()
            .HasForeignKey(x => x.TransporterProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}