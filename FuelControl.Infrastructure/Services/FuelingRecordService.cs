using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelingRecordService(
    FuelControlDbContext dbContext,
    IUserAuthorizationService authorization)
    : IFuelingRecordService
{
    public async Task<IReadOnlyList<FuelingRecord>> GetAllAsync(
        DateOnly date,
        TimeZoneInfo timeZone,
        Guid? fuelTruckId = null,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var localStart =
            date.ToDateTime(
                TimeOnly.MinValue);

        var localEnd =
            date.AddDays(1)
                .ToDateTime(
                    TimeOnly.MinValue);

        var utcStart =
            new DateTimeOffset(
                    localStart,
                    timeZone.GetUtcOffset(localStart))
                .ToUniversalTime();

        var utcEnd =
            new DateTimeOffset(
                    localEnd,
                    timeZone.GetUtcOffset(localEnd))
                .ToUniversalTime();

        var query =
            dbContext.FuelingRecords
                .AsNoTracking()
                .Include(x => x.FuelTruck)
                    .ThenInclude(x => x.Vehicle)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.Branch)
                .Include(x => x.Operator)
                    .ThenInclude(x => x.Branch)
                .Include(x => x.UssRecords)
                .Include(x => x.OmnicommRecord)
                .Where(x =>
                    x.FuelingDateTime >= utcStart &&
                    x.FuelingDateTime < utcEnd);

        var branchId =
            await authorization.GetCurrentUserBranchIdAsync(
                cancellationToken);

        if (branchId is not null &&
            !await authorization.IsAdminAsync(
                cancellationToken))
        {
            query = query.Where(x =>
                x.FuelTruck.Vehicle.BranchId == branchId.Value &&
                x.Vehicle.BranchId == branchId.Value &&
                x.Operator.BranchId == branchId.Value);
        }

        if (fuelTruckId.HasValue)
        {
            query = query.Where(x =>
                x.FuelTruckId == fuelTruckId.Value);
        }

        return await query
            .OrderBy(x => x.FuelingDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FuelingRecord>> GetByPeriodAsync(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        if (to < from)
        {
            throw new ArgumentException(
                "Дата окончания периода не может быть раньше даты начала.");
        }

        var localStart =
            from.ToDateTime(
                TimeOnly.MinValue);

        var localEnd =
            to.AddDays(1)
                .ToDateTime(
                    TimeOnly.MinValue);

        var utcStart =
            new DateTimeOffset(
                    localStart,
                    timeZone.GetUtcOffset(localStart))
                .ToUniversalTime();

        var utcEnd =
            new DateTimeOffset(
                    localEnd,
                    timeZone.GetUtcOffset(localEnd))
                .ToUniversalTime();

        var query =
            dbContext.FuelingRecords
                .AsNoTracking()
                .Include(x => x.FuelTruck)
                    .ThenInclude(x => x.Vehicle)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.Branch)
                .Include(x => x.Operator)
                    .ThenInclude(x => x.Branch)
                .Include(x => x.UssRecords)
                .Include(x => x.OmnicommRecord)
                .Where(x =>
                    x.FuelingDateTime >= utcStart &&
                    x.FuelingDateTime < utcEnd);

        var branchId =
            await authorization.GetCurrentUserBranchIdAsync(
                cancellationToken);

        if (branchId is not null &&
            !await authorization.IsAdminAsync(
                cancellationToken))
        {
            query = query.Where(x =>
                x.FuelTruck.Vehicle.BranchId == branchId.Value &&
                x.Vehicle.BranchId == branchId.Value &&
                x.Operator.BranchId == branchId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(x =>
                x.VehicleId == vehicleId.Value);
        }

        return await query
            .OrderBy(x => x.FuelingDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<FuelingRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var record =
            await dbContext.FuelingRecords
                .AsNoTracking()
                .Include(x => x.FuelTruck)
                    .ThenInclude(x => x.Vehicle)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.Branch)
                .Include(x => x.Operator)
                    .ThenInclude(x => x.Branch)
                .Include(x => x.UssRecords)
                .Include(x => x.OmnicommRecord)
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

        if (record is null)
        {
            return null;
        }

        await EnsureRecordAccessAsync(
            record,
            cancellationToken);

        return record;
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
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var userId =
            await authorization.GetCurrentUserIdAsync(
                cancellationToken);

        var fuelTruck =
            await dbContext.FuelTrucks
                .AsNoTracking()
                .Include(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == fuelTruckId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        await authorization.EnsureBranchAccessAsync(
            fuelTruck.Vehicle.BranchId,
            cancellationToken);

        var vehicle =
            await dbContext.Vehicles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == vehicleId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        await authorization.EnsureBranchAccessAsync(
            vehicle.BranchId,
            cancellationToken);

        var @operator =
            await dbContext.Operators
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == operatorId &&
                         x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Водитель не найден или отключён.");

        await authorization.EnsureBranchAccessAsync(
            @operator.BranchId,
            cancellationToken);

        var fuelingRecord =
            new FuelingRecord(
                fuelTruckId,
                vehicleId,
                operatorId,
                fuelingDateTime,
                volume,
                counterStart,
                counterEnd,
                userId);

        dbContext.FuelingRecords.Add(
            fuelingRecord);

        await dbContext.SaveChangesAsync(
            cancellationToken);

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
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var userId =
            await authorization.GetCurrentUserIdAsync(
                cancellationToken);

        var fuelingRecord =
            await dbContext.FuelingRecords
                .Include(x => x.FuelTruck)
                    .ThenInclude(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Заправка не найдена.");

        await EnsureRecordAccessAsync(
            fuelingRecord,
            cancellationToken);

        var fuelTruck =
            await dbContext.FuelTrucks
                .AsNoTracking()
                .Include(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == fuelTruckId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        await authorization.EnsureBranchAccessAsync(
            fuelTruck.Vehicle.BranchId,
            cancellationToken);

        var vehicle =
            await dbContext.Vehicles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == vehicleId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        await authorization.EnsureBranchAccessAsync(
            vehicle.BranchId,
            cancellationToken);

        var @operator =
            await dbContext.Operators
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == operatorId &&
                         x.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Водитель не найден или отключён.");

        await authorization.EnsureBranchAccessAsync(
            @operator.BranchId,
            cancellationToken);

        fuelingRecord.Update(
            fuelTruckId,
            vehicleId,
            operatorId,
            fuelingDateTime,
            volume,
            counterStart,
            counterEnd,
            userId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var fuelingRecord =
            await dbContext.FuelingRecords
                .Include(x => x.FuelTruck)
                    .ThenInclude(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Заправка не найдена.");

        await EnsureRecordAccessAsync(
            fuelingRecord,
            cancellationToken);

        dbContext.FuelingRecords.Remove(
            fuelingRecord);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task EnsureRecordAccessAsync(
        FuelingRecord record,
        CancellationToken cancellationToken)
    {
        if (await authorization.IsAdminAsync(
                cancellationToken))
        {
            return;
        }

        await authorization.EnsureBranchAccessAsync(
            record.FuelTruck.Vehicle.BranchId,
            cancellationToken);
    }
}