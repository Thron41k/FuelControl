using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class DeliveryMatchConfiguration : IEntityTypeConfiguration<DeliveryMatch>
{
    public void Configure(EntityTypeBuilder<DeliveryMatch> builder)
    {
        builder.ToTable("delivery_matches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OmnicommVehicleId).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.VolumeLiters).IsRequired();
        builder.Property(x => x.IsManual).IsRequired();
        builder.Property(x => x.MatchedAt).IsRequired();
        builder.Property(x => x.MatchedBy).IsRequired();

        builder.HasOne(x => x.FuelingRecord)
            .WithMany()
            .HasForeignKey(x => x.FuelingRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FuelingRecordId);
        builder.HasIndex(x => x.OmnicommVehicleId);
        builder.HasIndex(x => new { x.StartDate, x.EndDate });
    }
}