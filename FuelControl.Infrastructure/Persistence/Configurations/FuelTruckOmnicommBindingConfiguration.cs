using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelTruckOmnicommBindingConfiguration
    : IEntityTypeConfiguration<FuelTruckOmnicommBinding>
{
    public void Configure(
        EntityTypeBuilder<FuelTruckOmnicommBinding> builder)
    {
        builder.ToTable("fuel_truck_omnicomm_bindings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OmnicommObjectId)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .IsRequired();

        builder.HasOne(x => x.FuelTruck)
            .WithMany(x => x.OmnicommBindings)
            .HasForeignKey(x => x.FuelTruckId)
            .OnDelete(DeleteBehavior.Cascade);

        // Для одного топливозаправщика
        // на одну задачу может быть только один Omnicomm ID.
        builder.HasIndex(x => new
            {
                x.FuelTruckId,
                x.Purpose
            })
            .IsUnique();

        // Один и тот же терминал нельзя назначить
        // двум топливозаправщикам для одной задачи.
        builder.HasIndex(x => new
            {
                x.OmnicommObjectId,
                x.Purpose
            })
            .IsUnique();

        builder.HasIndex(x =>
            x.OmnicommObjectId);
    }
}