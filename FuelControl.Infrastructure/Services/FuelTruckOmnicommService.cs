using FuelControl.Domain.Entities;
using FuelControl.Domain.Enums;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelTruckOmnicommService(
    FuelControlDbContext dbContext)
    : IFuelTruckOmnicommService
{
    public async Task<IReadOnlyList<FuelTruckOmnicommBinding>> GetAllAsync(
        Guid fuelTruckId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<FuelTruckOmnicommBinding>()
            .AsNoTracking()
            .Where(x => x.FuelTruckId == fuelTruckId)
            .OrderBy(x => x.Purpose)
            .ToListAsync(cancellationToken);
    }

    public Task<long?> GetObjectIdAsync(
        Guid fuelTruckId,
        FuelTruckOmnicommPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Set<FuelTruckOmnicommBinding>()
            .AsNoTracking()
            .Where(x =>
                x.FuelTruckId == fuelTruckId &&
                x.Purpose == purpose)
            .Select(x => (long?)x.OmnicommObjectId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SetAsync(
        Guid fuelTruckId,
        long omnicommObjectId,
        FuelTruckOmnicommPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        if (fuelTruckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан топливозаправщик.",
                nameof(fuelTruckId));
        }

        if (omnicommObjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommObjectId));
        }

        var fuelTruckExists =
            await dbContext.FuelTrucks
                .AnyAsync(
                    x => x.Id == fuelTruckId,
                    cancellationToken);

        if (!fuelTruckExists)
        {
            throw new InvalidOperationException(
                "Топливозаправщик не найден.");
        }

        var existing =
            await dbContext.Set<FuelTruckOmnicommBinding>()
                .SingleOrDefaultAsync(
                    x =>
                        x.FuelTruckId == fuelTruckId &&
                        x.Purpose == purpose,
                    cancellationToken);

        if (existing is null)
        {
            var binding =
                new FuelTruckOmnicommBinding(
                    fuelTruckId,
                    omnicommObjectId,
                    purpose);

            dbContext.Set<FuelTruckOmnicommBinding>()
                .Add(binding);
        }
        else
        {
            existing.Update(
                omnicommObjectId,
                purpose);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RemoveAsync(
        Guid fuelTruckId,
        FuelTruckOmnicommPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var binding =
            await dbContext.Set<FuelTruckOmnicommBinding>()
                .SingleOrDefaultAsync(
                    x =>
                        x.FuelTruckId == fuelTruckId &&
                        x.Purpose == purpose,
                    cancellationToken);

        if (binding is null)
            return;

        dbContext.Set<FuelTruckOmnicommBinding>()
            .Remove(binding);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}