using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelingRecordService(
    FuelControlDbContext dbContext,
    ICurrentUserService currentUserService)
    : IFuelingRecordService
{
    public async Task<IReadOnlyList<FuelingRecord>> GetAllAsync(
        DateOnly date,
        Guid? fuelTruckId = null,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(
            date.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var end = start.AddDays(1);

        var query = dbContext.FuelingRecords
            .AsNoTracking()
            .Include(x => x.FuelTruck)
            .ThenInclude(x => x.Vehicle)
            .Include(x => x.Vehicle)
            .Include(x => x.Operator)
            .Include(x => x.UssRecords)
            .Where(x =>
                x.FuelingDateTime >= start &&
                x.FuelingDateTime < end);

        if (fuelTruckId.HasValue)
        {
            query = query.Where(x =>
                x.FuelTruckId == fuelTruckId.Value);
        }

        return await query
            .OrderBy(x => x.FuelingDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<FuelingRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FuelingRecords
            .AsNoTracking()
            .Include(x => x.FuelTruck)
                .ThenInclude(x => x.Vehicle)
            .Include(x => x.Vehicle)
            .Include(x => x.Operator)
            .Include(x => x.UssRecords)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var fuelTruck = await dbContext.FuelTrucks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == fuelTruckId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        var vehicleExists = await dbContext.Vehicles
            .AnyAsync(
                x => x.Id == vehicleId,
                cancellationToken);

        if (!vehicleExists)
        {
            throw new InvalidOperationException(
                "Техника не найдена.");
        }

        var operatorExists = await dbContext.Operators
            .AnyAsync(
                x => x.Id == operatorId &&
                     x.IsActive,
                cancellationToken);

        if (!operatorExists)
        {
            throw new InvalidOperationException(
                "Водитель не найден или отключён.");
        }

        var fuelingRecord = new FuelingRecord(
            fuelTruckId,
            vehicleId,
            operatorId,
            fuelingDateTime,
            volume,
            counterStart,
            counterEnd,
            userId);

        dbContext.FuelingRecords.Add(fuelingRecord);

        await dbContext.SaveChangesAsync(cancellationToken);

        return fuelingRecord.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var fuelingRecord = await dbContext.FuelingRecords
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Заправка не найдена.");

        var fuelTruckExists = await dbContext.FuelTrucks
            .AnyAsync(
                x => x.Id == fuelTruckId,
                cancellationToken);

        if (!fuelTruckExists)
        {
            throw new InvalidOperationException(
                "Топливозаправщик не найден.");
        }

        var vehicleExists = await dbContext.Vehicles
            .AnyAsync(
                x => x.Id == vehicleId,
                cancellationToken);

        if (!vehicleExists)
        {
            throw new InvalidOperationException(
                "Техника не найдена.");
        }

        var operatorExists = await dbContext.Operators
            .AnyAsync(
                x => x.Id == operatorId &&
                     x.IsActive,
                cancellationToken);

        if (!operatorExists)
        {
            throw new InvalidOperationException(
                "Водитель не найден или отключён.");
        }

        fuelingRecord.Update(
            fuelTruckId,
            vehicleId,
            operatorId,
            fuelingDateTime,
            volume,
            counterStart,
            counterEnd,
            userId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = GetCurrentUserId();

        var fuelingRecord = await dbContext.FuelingRecords
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Заправка не найдена.");

        dbContext.FuelingRecords.Remove(fuelingRecord);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid GetCurrentUserId()
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new UnauthorizedAccessException(
                "Пользователь не авторизован.");
        }

        return userId;
    }
}