using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelingRecordConfiguration : IEntityTypeConfiguration<FuelingRecord>
{
    public void Configure(EntityTypeBuilder<FuelingRecord> builder)
    {
        builder.ToTable("fueling_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Volume).IsRequired();
        builder.Property(x => x.CounterStart).IsRequired();
        builder.Property(x => x.CounterEnd).IsRequired();
        builder.Property(x => x.FuelingDateTime).IsRequired();

        builder.HasOne(x => x.FuelTruck)
            .WithMany()
            .HasForeignKey(x => x.FuelTruckId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FuelTruckId);
        builder.HasIndex(x => x.FuelingDateTime);
    }
}