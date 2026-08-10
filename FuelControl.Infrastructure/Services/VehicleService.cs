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
        var user = await GetRequiredUserAsync();

        var query = dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return await query
                .OrderBy(x => x.Branch.Name)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Dispatcher))
        {
            return [];
        }

        if (user.BranchId is null)
        {
            return [];
        }

        return await query
            .Where(x => x.BranchId == user.BranchId.Value)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            throw new UnauthorizedAccessException(
                "Только администратор может окончательно удалить технику.");
        }

        var vehicle = await dbContext.Vehicles
                          .SingleOrDefaultAsync(
                              x => x.Id == id,
                              cancellationToken)
                      ?? throw new InvalidOperationException(
                          "Техника не найдена.");

        if (vehicle.IsActive)
        {
            throw new InvalidOperationException(
                "Активную технику нельзя удалить. Сначала отключите её.");
        }

        var hasFuelingRecords = await dbContext.Set<FuelingRecord>()
            .AnyAsync(
                x => x.VehicleId == id,
                cancellationToken);

        if (hasFuelingRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить технику, поскольку с ней связаны записи заправок.");
        }

        dbContext.Vehicles.Remove(vehicle);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var query = dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.Id == id);

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return await query.SingleOrDefaultAsync(cancellationToken);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Dispatcher))
        {
            return null;
        }

        if (user.BranchId is null)
        {
            return null;
        }

        return await query
            .Where(x => x.BranchId == user.BranchId.Value)
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
            name.Trim(),
            registrationNumber.Trim(),
            targetBranchId,
            NormalizeInventoryNumber(inventoryNumber));

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
            await userManager.IsInRoleAsync(user, Roles.Admin);

        if (isAdmin)
        {
            if (branchId is null)
            {
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(branchId));
            }

            vehicle.Update(
                name.Trim(),
                registrationNumber.Trim(),
                branchId.Value,
                NormalizeInventoryNumber(inventoryNumber));

            await dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Dispatcher))
        {
            throw new UnauthorizedAccessException();
        }

        if (user.BranchId is null)
        {
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");
        }

        if (vehicle.BranchId != user.BranchId.Value)
        {
            throw new UnauthorizedAccessException(
                "Техника относится к другому филиалу.");
        }

        // Принципиально важно:
        // branchId из браузера здесь НЕ используется.
        vehicle.Update(
            name.Trim(),
            registrationNumber.Trim(),
            user.BranchId.Value,
            NormalizeInventoryNumber(inventoryNumber));

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

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            SetVehicleState(vehicle, isActive);

            await dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Dispatcher))
        {
            throw new UnauthorizedAccessException();
        }

        if (user.BranchId is null)
        {
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");
        }

        if (vehicle.BranchId != user.BranchId.Value)
        {
            throw new UnauthorizedAccessException(
                "Техника относится к другому филиалу.");
        }

        SetVehicleState(vehicle, isActive);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> GetRequiredUserAsync()
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new UnauthorizedAccessException(
                "Пользователь не авторизован.");
        }

        return await userManager.FindByIdAsync(
                   userId.ToString())
               ?? throw new UnauthorizedAccessException(
                   "Пользователь не найден.");
    }

    private async Task<Guid> ResolveBranchForWriteAsync(
        ApplicationUser user,
        Guid? requestedBranchId)
    {
        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            if (requestedBranchId is null)
            {
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(requestedBranchId));
            }

            return requestedBranchId.Value;
        }

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Dispatcher))
        {
            throw new UnauthorizedAccessException();
        }

        if (user.BranchId is null)
        {
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");
        }

        // Никогда не используем requestedBranchId
        // для Dispatcher.
        return user.BranchId.Value;
    }

    private static void SetVehicleState(
        Vehicle vehicle,
        bool isActive)
    {
        if (isActive)
        {
            vehicle.Activate();
        }
        else
        {
            vehicle.Deactivate();
        }
    }

    private static string? NormalizeInventoryNumber(
        string? inventoryNumber)
    {
        return string.IsNullOrWhiteSpace(inventoryNumber)
            ? null
            : inventoryNumber.Trim();
    }
}