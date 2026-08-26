using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelingUssRecordConfiguration
    : IEntityTypeConfiguration<FuelingUssRecord>
{
    public void Configure(
        EntityTypeBuilder<FuelingUssRecord> builder)
    {
        builder.ToTable("fueling_uss_records");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
            {
                x.OmnicommReportId,
                x.OmnicommEventId
            })
            .IsUnique();

        builder.Property(x => x.OmnicommReportId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.OmnicommFuelTruckId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.VolumeLiters)
            .HasPrecision(12, 3)
            .IsRequired();

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        /*
         * Связь с FuelingRecord.
         *
         * Одна заправка может иметь несколько
         * показаний УСС.
         */
        builder.HasOne(x => x.FuelingRecord)
            .WithMany(x => x.UssRecords)
            .HasForeignKey(x => x.FuelingRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        /*
         * Одно событие Omnicomm может быть
         * привязано только к одной заправке.
         */
        builder.HasIndex(x => x.OmnicommEventId)
            .IsUnique();

        builder.HasIndex(x => x.FuelingRecordId);

        builder.HasIndex(x => x.OmnicommReportId);

        builder.HasIndex(x => x.OmnicommFuelTruckId);

        /*
         * Индекс для выборки показаний УСС
         * конкретного топливозаправщика.
         */
        builder.HasIndex(x => new
        {
            x.OmnicommFuelTruckId,
            x.StartDate
        });
    }
}