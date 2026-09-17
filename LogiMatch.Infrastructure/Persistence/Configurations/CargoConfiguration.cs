using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class CargoConfiguration
    : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("cargos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.WeightKg)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.VolumeM3)
            .HasPrecision(12, 3)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.RequiresRefrigeration)
            .IsRequired();

        builder.Property(x => x.RequiresTailLift)
            .IsRequired();

        builder.HasOne<TransportRequest>()
            .WithMany(x => x.Cargos)
            .HasForeignKey(x => x.TransportRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TransportRequestId);
    }
}