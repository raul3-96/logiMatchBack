using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class TripCargoConfiguration
    : IEntityTypeConfiguration<TripCargo>
{
    public void Configure(EntityTypeBuilder<TripCargo> builder)
    {
        builder.ToTable("trip_cargos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TripId)
            .IsRequired();

        builder.Property(x => x.TransportRequestId)
            .IsRequired();

        builder.Property(x => x.WeightKg)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.VolumeM3)
            .HasPrecision(12, 3)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TransportRequest>()
            .WithMany()
            .HasForeignKey(x => x.TransportRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TripId);

        builder.HasIndex(x => x.TransportRequestId)
            .IsUnique()
            .HasFilter("\"Status\" <> 4");
    }
}