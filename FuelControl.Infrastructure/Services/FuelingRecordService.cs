using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelingRecordService(FuelControlDbContext db)
{
    public async Task<List<FuelingRecord>> GetAsync(
        Guid? fuelTruckId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = db.FuelingRecords
            .Include(x => x.FuelTruck)
            .Include(x => x.Vehicle)
            .Include(x => x.Operator)
            .AsQueryable();

        if (fuelTruckId is not null)
            query = query.Where(x => x.FuelTruckId == fuelTruckId);

        if (from is not null)
        {
            var fromUtc = from.Value.ToUniversalTime();
            query = query.Where(x => x.FuelingDateTime >= fromUtc);
        }

        if (to is not null)
        {
            var toUtc = to.Value.ToUniversalTime();
            query = query.Where(x => x.FuelingDateTime < toUtc);
        }

        return await query
            .OrderByDescending(x => x.FuelingDateTime)
            .ToListAsync();
    }

    public Task<FuelingRecord?> GetByIdAsync(Guid id) =>
        db.FuelingRecords
            .Include(x => x.FuelTruck)
            .Include(x => x.Vehicle)
            .Include(x => x.Operator)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task CreateAsync(
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        Guid userId)
    {
        var record = new FuelingRecord(
            fuelTruckId,
            vehicleId,
            operatorId,
            fuelingDateTime.ToUniversalTime(),
            volume,
            counterStart,
            counterEnd,
            userId);

        db.FuelingRecords.Add(record);
        await db.SaveChangesAsync();
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
        Guid userId)
    {
        var record = await db.FuelingRecords.FindAsync(id)
            ?? throw new InvalidOperationException("Запись не найдена.");

        record.Update(
            fuelTruckId,
            vehicleId,
            operatorId,
            fuelingDateTime.ToUniversalTime(),
            volume,
            counterStart,
            counterEnd,
            userId);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var record = await db.FuelingRecords.FindAsync(id);
        if (record is null) return;

        db.FuelingRecords.Remove(record);
        await db.SaveChangesAsync();
    }
}