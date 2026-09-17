using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransportRequestId)
            .IsRequired();

        builder.Property(x => x.TransportOfferId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasOne<TransportRequest>()
            .WithMany()
            .HasForeignKey(x => x.TransportRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TransportOffer>()
            .WithMany()
            .HasForeignKey(x => x.TransportOfferId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una solicitud solo puede tener un Booking activo.
        // Los Bookings cancelados se conservan como histórico.
        builder.HasIndex(x => x.TransportRequestId)
            .IsUnique()
            .HasFilter("\"Status\" <> 4");

        // Una oferta solo puede estar asociada a un Booking.
        builder.HasIndex(x => x.TransportOfferId)
            .IsUnique();
    }
}