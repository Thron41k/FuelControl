using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelTruckConfiguration
    : IEntityTypeConfiguration<FuelTruck>
{
    public void Configure(EntityTypeBuilder<FuelTruck> builder)
    {
        builder.ToTable("fuel_trucks");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.VehicleId)
            .IsUnique();
    }
}