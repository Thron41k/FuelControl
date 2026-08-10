using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class DirectoryService(FuelControlDbContext db)
{
    // —— АТЗ ——
    public Task<List<FuelTruck>> GetFuelTrucksAsync(bool onlyActive = true) =>
        db.FuelTrucks
            .Where(x => !onlyActive || x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

    public async Task CreateFuelTruckAsync(string name, string regNumber, Guid branchId, string? inventory)
    {
        db.FuelTrucks.Add(new FuelTruck(name, regNumber, branchId, inventory));
        await db.SaveChangesAsync();
    }

    public async Task UpdateFuelTruckAsync(Guid id, string name, string regNumber, Guid branchId, string? inventory)
    {
        var entity = await db.FuelTrucks.FindAsync(id)
            ?? throw new InvalidOperationException("АТЗ не найден");
        entity.Update(name, regNumber, branchId, inventory);
        await db.SaveChangesAsync();
    }

    // —— Техника ——
    public Task<List<Vehicle>> GetVehiclesAsync(bool onlyActive = true) =>
        db.Vehicles
            .Where(x => !onlyActive || x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

    public async Task CreateVehicleAsync(string name, string regNumber, Guid branchId, string? inventory)
    {
        db.Vehicles.Add(new Vehicle(name, regNumber, branchId, inventory));
        await db.SaveChangesAsync();
    }

    public async Task UpdateVehicleAsync(Guid id, string name, string regNumber, Guid branchId, string? inventory)
    {
        var entity = await db.Vehicles.FindAsync(id)
            ?? throw new InvalidOperationException("Техника не найдена");
        entity.Update(name, regNumber, branchId, inventory);
        await db.SaveChangesAsync();
    }

    // —— Машинисты ——
    public Task<List<Operator>> GetOperatorsAsync(bool onlyActive = true) =>
        db.Operators
            .Where(x => !onlyActive || x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync();

    public async Task CreateOperatorAsync(string fullName, Guid branchId, string? personnelNumber)
    {
        db.Operators.Add(new Operator(fullName, branchId, personnelNumber));
        await db.SaveChangesAsync();
    }

    public async Task UpdateOperatorAsync(Guid id, string fullName, Guid branchId, string? personnelNumber)
    {
        var entity = await db.Operators.FindAsync(id)
            ?? throw new InvalidOperationException("Машинист не найден");
        entity.Update(fullName, branchId, personnelNumber);
        await db.SaveChangesAsync();
    }
}
