using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class DirectoryService(FuelControlDbContext db)
{


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
