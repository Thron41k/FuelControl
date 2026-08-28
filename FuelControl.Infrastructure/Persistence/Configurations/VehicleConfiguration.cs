using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RegistrationNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InventoryNumber)
            .HasMaxLength(50);

        builder.Property(x => x.RfidTagId)
            .HasMaxLength(64);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.RegistrationNumber)
            .IsUnique();

        builder.HasIndex(x => x.OmnicommObjectId)
            .IsUnique()
            .HasFilter("\"OmnicommObjectId\" IS NOT NULL");

        builder.HasIndex(x => x.RfidTagId)
            .IsUnique()
            .HasFilter("\"RfidTagId\" IS NOT NULL");

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);
    }
}