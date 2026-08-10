using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Persistence;

public sealed class FuelControlDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public FuelControlDbContext(DbContextOptions<FuelControlDbContext> options)
        : base(options)
    {
    }

    public DbSet<FuelTruck> FuelTrucks => Set<FuelTruck>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<FuelingRecord> FuelingRecords => Set<FuelingRecord>();
    public DbSet<DeliveryMatch> DeliveryMatches => Set<DeliveryMatch>();
    public DbSet<FuelEventMatch> FuelEventMatches => Set<FuelEventMatch>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(FuelControlDbContext).Assembly);
    }
}