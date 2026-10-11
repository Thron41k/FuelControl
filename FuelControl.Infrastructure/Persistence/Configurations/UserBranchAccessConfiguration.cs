using FuelControl.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuelControl.Infrastructure.Persistence.Configurations;

public sealed class UserBranchAccessConfiguration
    : IEntityTypeConfiguration<UserBranchAccess>
{
    public void Configure(
        EntityTypeBuilder<UserBranchAccess> builder)
    {
        builder.ToTable("UserBranchAccesses");

        builder.HasKey(x => new
        {
            x.UserId,
            x.BranchId
        });

        builder.HasOne(x => x.User)
            .WithMany(x => x.AdditionalBranchAccesses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);
    }
}