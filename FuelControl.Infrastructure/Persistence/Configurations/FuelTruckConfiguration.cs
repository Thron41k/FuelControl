using FuelControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class FuelTruckConfiguration : IEntityTypeConfiguration<FuelTruck>
{
    public void Configure(EntityTypeBuilder<FuelTruck> builder)
    {
        builder.ToTable("fuel_trucks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RegistrationNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InventoryNumber).HasMaxLength(50);

        builder.HasIndex(x => x.OmnicommObjectId);
        builder.HasIndex(x => x.RegistrationNumber).IsUnique();
        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);
    }
}
