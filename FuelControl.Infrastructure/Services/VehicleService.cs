using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class VehicleService(
    FuelControlDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
    : IVehicleService
{
    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
            return [];

        var query = dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            return await query
                .OrderBy(x => x.Branch.Name)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        if (!await userManager.IsInRoleAsync(user, "Dispatcher"))
            return [];

        if (user.BranchId is null)
            return [];

        return await query
            .Where(x => x.BranchId == user.BranchId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
            return null;

        var query = dbContext.Vehicles
            .Include(x => x.Branch)
            .Where(x => x.Id == id);

        if (await userManager.IsInRoleAsync(user, "Admin"))
            return await query.SingleOrDefaultAsync(cancellationToken);

        if (!await userManager.IsInRoleAsync(user, "Dispatcher"))
            return null;

        if (user.BranchId is null)
            return null;

        return await query
            .Where(x => x.BranchId == user.BranchId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        string name,
        string registrationNumber,
        Guid? branchId,
        string? inventoryNumber,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var targetBranchId = await ResolveBranchForWriteAsync(
            user,
            branchId);

        var vehicle = new Vehicle(
            name,
            registrationNumber,
            targetBranchId,
            inventoryNumber);

        dbContext.Vehicles.Add(vehicle);

        await dbContext.SaveChangesAsync(cancellationToken);

        return vehicle.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string registrationNumber,
        Guid? branchId,
        string? inventoryNumber,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var vehicle = await dbContext.Vehicles
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        var isAdmin =
            await userManager.IsInRoleAsync(user, "Admin");

        if (!isAdmin)
        {
            if (!await userManager.IsInRoleAsync(
                    user,
                    "Dispatcher"))
            {
                throw new UnauthorizedAccessException();
            }

            if (user.BranchId is null ||
                vehicle.BranchId != user.BranchId)
            {
                throw new UnauthorizedAccessException(
                    "Техника относится к другому филиалу.");
            }

            // Диспетчер не может перенести технику
            // в другой филиал.
            branchId = user.BranchId;
        }
        else
        {
            if (branchId is null)
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(branchId));
        }

        vehicle.Update(
            name,
            registrationNumber,
            branchId!.Value,
            inventoryNumber);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var vehicle = await dbContext.Vehicles
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        var isAdmin =
            await userManager.IsInRoleAsync(user, "Admin");

        if (!isAdmin)
        {
            if (!await userManager.IsInRoleAsync(
                    user,
                    "Dispatcher"))
            {
                throw new UnauthorizedAccessException();
            }

            if (user.BranchId is null ||
                vehicle.BranchId != user.BranchId)
            {
                throw new UnauthorizedAccessException(
                    "Техника относится к другому филиалу.");
            }
        }

        if (isActive)
            vehicle.Activate();
        else
            vehicle.Deactivate();

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (currentUserService.UserId is not { } userId)
            return null;

        return await userManager.FindByIdAsync(
            userId.ToString());
    }

    private async Task<ApplicationUser> GetRequiredUserAsync()
    {
        var user = await GetCurrentUserAsync();

        return user
            ?? throw new UnauthorizedAccessException(
                "Пользователь не авторизован.");
    }

    private async Task<Guid> ResolveBranchForWriteAsync(
        ApplicationUser user,
        Guid? requestedBranchId)
    {
        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            if (requestedBranchId is null)
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(requestedBranchId));

            return requestedBranchId.Value;
        }

        if (!await userManager.IsInRoleAsync(
                user,
                "Dispatcher"))
        {
            throw new UnauthorizedAccessException();
        }

        if (user.BranchId is null)
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");

        return user.BranchId.Value;
    }
}