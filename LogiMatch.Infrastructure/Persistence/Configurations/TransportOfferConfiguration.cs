using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class TransportOfferConfiguration
    : IEntityTypeConfiguration<TransportOffer>
{
    public void Configure(EntityTypeBuilder<TransportOffer> builder)
    {
        builder.ToTable("transport_offers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.EstimatedPickupDate)
            .IsRequired();

        builder.Property(x => x.EstimatedDeliveryDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<TransportRequest>()
            .WithMany()
            .HasForeignKey(x => x.TransportRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TransporterProfile>()
            .WithMany()
            .HasForeignKey(x => x.TransporterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TransportRequestId);

        builder.HasIndex(x => x.TransporterProfileId);

        builder.HasIndex(x => x.VehicleId);

        builder.HasIndex(x => x.Status);
    }
}