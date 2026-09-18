using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class TripConfiguration
    : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransporterProfileId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.OriginLocationId)
            .IsRequired();

        builder.Property(x => x.DestinationLocationId)
            .IsRequired();

        builder.Property(x => x.DepartureDate)
            .IsRequired();

        builder.Property(x => x.EstimatedArrivalDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.AvailableWeightKg)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.AvailableVolumeM3)
            .HasPrecision(12, 3)
            .IsRequired();

        builder.HasOne<TransporterProfile>()
            .WithMany()
            .HasForeignKey(x => x.TransporterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.OriginLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.DestinationLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TransporterProfileId);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.OriginLocationId);
        builder.HasIndex(x => x.DestinationLocationId);
        builder.HasIndex(x => x.DepartureDate);
    }
}