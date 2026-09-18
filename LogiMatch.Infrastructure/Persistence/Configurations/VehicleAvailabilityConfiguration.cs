using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class VehicleAvailabilityConfiguration
    : IEntityTypeConfiguration<VehicleAvailability>
{
    public void Configure(EntityTypeBuilder<VehicleAvailability> builder)
    {
        builder.ToTable("vehicle_availabilities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.AvailableFrom)
            .IsRequired();

        builder.Property(x => x.AvailableTo)
            .IsRequired();

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.AvailableFrom);
        builder.HasIndex(x => x.AvailableTo);
    }
}