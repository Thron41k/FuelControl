using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelingRecordConfiguration
    : IEntityTypeConfiguration<FuelingRecord>
{
    public void Configure(EntityTypeBuilder<FuelingRecord> builder)
    {
        builder.ToTable("fueling_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FuelingDateTime)
            .IsRequired();

        builder.Property(x => x.Volume)
            .IsRequired();

        builder.Property(x => x.CounterStart)
            .IsRequired();

        builder.Property(x => x.CounterEnd)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy);

        // Топливозаправщик
        builder.HasOne(x => x.FuelTruck)
            .WithMany()
            .HasForeignKey(x => x.FuelTruckId)
            .OnDelete(DeleteBehavior.Restrict);

        // Заправляемая техника
        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Водитель
        builder.HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Показания УСС
        builder.HasMany(x => x.UssRecords)
            .WithOne(x => x.FuelingRecord)
            .HasForeignKey(x => x.FuelingRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FuelTruckId);

        builder.HasIndex(x => x.VehicleId);

        builder.HasIndex(x => x.OperatorId);

        builder.HasIndex(x => x.FuelingDateTime);
    }
}