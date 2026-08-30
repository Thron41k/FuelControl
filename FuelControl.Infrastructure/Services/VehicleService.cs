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

        return await query
            .Where(x =>
                x.BranchId == user.BranchId.Value)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }


    public async Task<Vehicle?> GetByIdAsync(
        Guid id,
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
                .Where(x => x.Id == id);

        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            return await query
                .SingleOrDefaultAsync(
                    cancellationToken);
        }

        if (user.BranchId is null)
        {
            return null;
        }

        return await query
            .Where(x =>
                x.BranchId == user.BranchId.Value)
            .SingleOrDefaultAsync(
                cancellationToken);
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
                branchId);

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

        // Administrator может изменять технику
        // любого филиала.
        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            if (branchId is null)
            {
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(branchId));
            }

            await EnsureOmnicommObjectIsAvailableAsync(
                omnicommObjectId,
                id,
                cancellationToken);

            var normalizedRfidTagId =
                NormalizeRfidTagId(rfidTagId);

            await EnsureRfidTagIsAvailableAsync(
                normalizedRfidTagId,
                id,
                cancellationToken);

            vehicle.Update(
                name.Trim(),
                registrationNumber.Trim(),
                branchId.Value,
                omnicommObjectId,
                NormalizeInventoryNumber(
                    inventoryNumber),
                normalizedRfidTagId);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return;
        }

        // Здесь уже гарантирован Moderator
        // или выше, поскольку был EnsureModeratorAsync().
        if (user.BranchId is null)
        {
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");
        }

        // Moderator не может редактировать технику
        // другого филиала.
        if (vehicle.BranchId !=
            user.BranchId.Value)
        {
            throw new UnauthorizedAccessException(
                "Техника относится к другому филиалу.");
        }

        var normalizedModeratorRfidTagId =
            NormalizeRfidTagId(rfidTagId);

        await EnsureOmnicommObjectIsAvailableAsync(
            omnicommObjectId,
            id,
            cancellationToken);

        await EnsureRfidTagIsAvailableAsync(
            normalizedModeratorRfidTagId,
            id,
            cancellationToken);

        // Moderator не может изменить филиал.
        vehicle.Update(
            name.Trim(),
            registrationNumber.Trim(),
            user.BranchId.Value,
            omnicommObjectId,
            NormalizeInventoryNumber(
                inventoryNumber),
            normalizedModeratorRfidTagId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
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

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            if (user.BranchId is null)
            {
                throw new UnauthorizedAccessException(
                    "Для пользователя не назначен филиал.");
            }

            if (vehicle.BranchId !=
                user.BranchId.Value)
            {
                throw new UnauthorizedAccessException(
                    "Техника относится к другому филиалу.");
            }
        }

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
        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            if (user.BranchId is null)
            {
                throw new UnauthorizedAccessException(
                    "Для пользователя не назначен филиал.");
            }

            if (vehicle.BranchId !=
                user.BranchId.Value)
            {
                throw new UnauthorizedAccessException(
                    "Техника относится к другому филиалу.");
            }
        }

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

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            if (user.BranchId is null)
            {
                return [];
            }

            query =
                query.Where(
                    x => x.BranchId ==
                         user.BranchId.Value);
        }

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


    private async Task<Guid>
        ResolveBranchForWriteAsync(
            ApplicationUser user,
            Guid? requestedBranchId)
    {
        // Admin может назначить любой филиал.
        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            if (requestedBranchId is null)
            {
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(requestedBranchId));
            }

            return requestedBranchId.Value;
        }

        // Moderator работает только со своим филиалом.
        if (user.BranchId is null)
        {
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");
        }

        return user.BranchId.Value;
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