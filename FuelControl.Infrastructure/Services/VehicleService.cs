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
    ICurrentUserService currentUserService,
    IUserAuthorizationService authorization)
    : IVehicleService
{
    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var user =
            await GetRequiredUserAsync();

        var query =
            dbContext.Vehicles
                .AsNoTracking()
                .Include(x => x.Branch)
                .AsQueryable();

        if (!includeInactive)
        {
            query =
                query.Where(x => x.IsActive);
        }

        // Administrator видит всю технику.
        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            return await query
                .OrderBy(x => x.Branch.Name)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        // Dispatcher и Moderator видят технику
        // только своего филиала.
        if (user.BranchId is null)
        {
            return [];
        }

        var accessibleBranchIds =
            await authorization.GetAccessibleBranchIdsAsync(
                cancellationToken);

        return await query
            .Where(x => accessibleBranchIds.Contains(x.BranchId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }


    public async Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var accessibleBranchIds =
            await authorization.GetAccessibleBranchIdsAsync(
                cancellationToken);

        return await dbContext.Vehicles
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x =>
                x.Id == id &&
                accessibleBranchIds.Contains(x.BranchId))
            .SingleOrDefaultAsync(cancellationToken);
    }


    public async Task<Guid> CreateAsync(
        string name,
        string registrationNumber,
        Guid? branchId,
        string? inventoryNumber,
        long? omnicommObjectId,
        string? rfidTagId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user =
            await GetRequiredUserAsync();

        var targetBranchId =
            await ResolveBranchForWriteAsync(
                user,
                branchId, cancellationToken);

        var normalizedRfidTagId =
            NormalizeRfidTagId(rfidTagId);

        await EnsureOmnicommObjectIsAvailableAsync(
            omnicommObjectId,
            null,
            cancellationToken);

        await EnsureRfidTagIsAvailableAsync(
            normalizedRfidTagId,
            null,
            cancellationToken);

        var vehicle =
            new Vehicle(
                name.Trim(),
                registrationNumber.Trim(),
                targetBranchId,
                omnicommObjectId,
                NormalizeInventoryNumber(
                    inventoryNumber),
                normalizedRfidTagId);

        dbContext.Vehicles.Add(vehicle);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return vehicle.Id;
    }


    public async Task UpdateAsync(
        Guid id,
        string name,
        string registrationNumber,
        Guid? branchId,
        string? inventoryNumber,
        long? omnicommObjectId,
        string? rfidTagId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user =
            await GetRequiredUserAsync();

        var vehicle =
            await dbContext.Vehicles
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        await authorization.EnsureBranchAccessAsync(
            vehicle.BranchId,
            cancellationToken);

        var targetBranchId =
            await ResolveBranchForWriteAsync(
                user,
                branchId ?? vehicle.BranchId, cancellationToken);

        var normalizedRfidTagId =
            NormalizeRfidTagId(rfidTagId);

        await EnsureOmnicommObjectIsAvailableAsync(
            omnicommObjectId,
            id,
            cancellationToken);

        await EnsureRfidTagIsAvailableAsync(
            normalizedRfidTagId,
            id,
            cancellationToken);

        vehicle.Update(
            name.Trim(),
            registrationNumber.Trim(),
            targetBranchId,
            omnicommObjectId,
            NormalizeInventoryNumber(inventoryNumber),
            normalizedRfidTagId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user =
            await GetRequiredUserAsync();

        var vehicle =
            await dbContext.Vehicles
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        await authorization.EnsureBranchAccessAsync(
            vehicle.BranchId,
            cancellationToken);

        SetVehicleState(
            vehicle,
            isActive);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }


    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user =
            await GetRequiredUserAsync();

        var vehicle =
            await dbContext.Vehicles
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена.");

        // Moderator работает только со своим филиалом.
        await authorization.EnsureBranchAccessAsync(
            vehicle.BranchId,
            cancellationToken);

        if (vehicle.IsActive)
        {
            throw new InvalidOperationException(
                "Активную технику нельзя удалить. " +
                "Сначала отключите её.");
        }

        var isFuelTruck =
            await dbContext
                .Set<FuelTruck>()
                .AnyAsync(
                    x => x.VehicleId == id,
                    cancellationToken);

        if (isFuelTruck)
        {
            throw new InvalidOperationException(
                "Нельзя удалить технику, поскольку она " +
                "числится топливозаправщиком. Сначала " +
                "удалите её из справочника топливозаправщиков.");
        }

        var hasFuelingRecords =
            await dbContext
                .Set<FuelingRecord>()
                .AnyAsync(
                    x => x.VehicleId == id,
                    cancellationToken);

        if (hasFuelingRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить технику, поскольку с ней " +
                "связаны записи заправок.");
        }

        dbContext.Vehicles.Remove(
            vehicle);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }


    public async Task<IReadOnlyCollection<long>>
        GetExistingOmnicommVehicleIdsAsync(
            CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var user =
            await GetRequiredUserAsync();

        var query =
            dbContext.Vehicles
                .AsNoTracking()
                .Where(x =>
                    x.OmnicommObjectId != null);

        var accessibleBranchIds =
            await authorization.GetAccessibleBranchIdsAsync(
                cancellationToken);

        query = query.Where(
            x => accessibleBranchIds.Contains(x.BranchId));

        return await query
            .Select(
                x => x.OmnicommObjectId!.Value)
            .ToListAsync(
                cancellationToken);
    }


    private async Task<ApplicationUser>
        GetRequiredUserAsync()
    {
        if (currentUserService.UserId
            is not { } userId)
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
        Guid? requestedBranchId,
        CancellationToken cancellationToken)
    {
        var targetBranchId = requestedBranchId;

        if (targetBranchId is null &&
            !await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            targetBranchId = user.BranchId;
        }

        if (targetBranchId is null)
        {
            throw new ArgumentException(
                "Необходимо указать филиал.",
                nameof(requestedBranchId));
        }

        var branchExists = await dbContext.Branchs
            .AnyAsync(
                x => x.Id == targetBranchId.Value,
                cancellationToken);

        if (!branchExists)
        {
            throw new InvalidOperationException(
                "Указанный филиал не найден.");
        }

        await authorization.EnsureBranchAccessAsync(
            targetBranchId.Value,
            cancellationToken);

        return targetBranchId.Value;
    }


    private async Task
        EnsureOmnicommObjectIsAvailableAsync(
            long? omnicommObjectId,
            Guid? vehicleId,
            CancellationToken cancellationToken)
    {
        if (omnicommObjectId is null)
        {
            return;
        }

        var exists =
            await dbContext.Vehicles
                .AnyAsync(
                    x =>
                        x.OmnicommObjectId ==
                        omnicommObjectId.Value &&
                        (vehicleId == null ||
                         x.Id != vehicleId.Value),
                    cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "Техника с таким объектом Omnicomm " +
                "уже добавлена.");
        }
    }


    private async Task
        EnsureRfidTagIsAvailableAsync(
            string? rfidTagId,
            Guid? excludedVehicleId,
            CancellationToken cancellationToken)
    {
        if (rfidTagId is null)
        {
            return;
        }

        var vehicleExists =
            await dbContext.Vehicles
                .AnyAsync(
                    x =>
                        x.RfidTagId == rfidTagId &&
                        (excludedVehicleId == null ||
                         x.Id != excludedVehicleId.Value),
                    cancellationToken);

        if (vehicleExists)
        {
            throw new InvalidOperationException(
                "RFID метка уже назначена другой технике.");
        }

        var operatorExists =
            await dbContext.Operators
                .AnyAsync(
                    x => x.RfidTagId == rfidTagId,
                    cancellationToken);

        if (operatorExists)
        {
            throw new InvalidOperationException(
                "RFID метка уже назначена водителю.");
        }
    }


    private static string?
        NormalizeInventoryNumber(
            string? inventoryNumber)
    {
        return string.IsNullOrWhiteSpace(
                inventoryNumber)
            ? null
            : inventoryNumber.Trim();
    }


    private static string?
        NormalizeRfidTagId(
            string? rfidTagId)
    {
        if (string.IsNullOrWhiteSpace(
                rfidTagId))
        {
            return null;
        }

        return rfidTagId
            .Trim()
            .ToUpperInvariant();
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
}