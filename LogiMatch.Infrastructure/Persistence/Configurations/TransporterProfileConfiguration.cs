using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMatch.Infrastructure.Persistence.Configurations;

public class TransporterProfileConfiguration
    : IEntityTypeConfiguration<TransporterProfile>
{
    public void Configure(EntityTypeBuilder<TransporterProfile> builder)
    {
        builder.ToTable("transporter_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<TransporterProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}