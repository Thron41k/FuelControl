using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelingOmnicommRecordConfiguration
    : IEntityTypeConfiguration<FuelingOmnicommRecord>
{
    public void Configure(
        EntityTypeBuilder<FuelingOmnicommRecord> builder)
    {
        builder.ToTable("fueling_omnicomm_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OmnicommEventId)
            .IsRequired();

        builder.Property(x => x.OmnicommReportId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OmnicommVehicleId)
            .IsRequired();

        builder.Property(x => x.VehicleName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.VolumeLiters)
            .IsRequired()
            .HasPrecision(12, 3);

        builder.Property(x => x.MatchedAt)
            .IsRequired();

        builder.Property(x => x.MatchedBy)
            .IsRequired();

        // Наша заправка
        builder.HasOne(x => x.FuelingRecord)
            .WithOne(x => x.OmnicommRecord)
            .HasForeignKey<FuelingOmnicommRecord>(
                x => x.FuelingRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // Одна наша заправка может иметь только
        // одну привязанную запись Omnicomm.
        builder.HasIndex(x => x.FuelingRecordId)
            .IsUnique();

        // Защита от повторного импорта одного события Omnicomm.
        builder.HasIndex(
                x => new
                {
                    x.OmnicommReportId,
                    x.OmnicommEventId
                })
            .IsUnique();

        builder.HasIndex(x => x.OmnicommVehicleId);

        builder.HasIndex(x => x.StartDate);

        builder.HasIndex(x => x.EndDate);
    }
}