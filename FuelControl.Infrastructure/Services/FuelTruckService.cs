using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelTruckService(
    FuelControlDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService,
    IUserAuthorizationService authorization)
    : IFuelTruckService
{
    public async Task<IReadOnlyList<FuelTruck>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);


        var query = dbContext.Set<FuelTruck>()
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .ThenInclude(x => x.Branch)
            .Include(x => x.UssVehicle)
            .Include(x => x.TankVehicle)
            .AsQueryable();

        var accessibleBranchIds =
            await authorization.GetAccessibleBranchIdsAsync(
                cancellationToken);

        return await query
            .Where(x =>
                accessibleBranchIds.Contains(x.Vehicle.BranchId))
            .OrderBy(x => x.Vehicle.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> GetAvailableVehiclesAsync(
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var query = dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.IsActive &&
                !dbContext.Set<FuelTruck>()
                    .Any(x => x.VehicleId == vehicle.Id))
            .Include(x => x.Branch)
            .AsQueryable();

        var accessibleBranchIds =
            await authorization.GetAccessibleBranchIdsAsync(
                cancellationToken);

        return await query
            .Where(x => accessibleBranchIds.Contains(x.BranchId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user = await GetRequiredUserAsync();

        var vehicle = await dbContext.Vehicles
            .SingleOrDefaultAsync(
                x => x.Id == vehicleId &&
                     x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена или неактивна.");

        await EnsureVehicleBelongsToUserScopeAsync(
            vehicle.BranchId);

        var alreadyExists = await dbContext
            .Set<FuelTruck>()
            .AnyAsync(
                x => x.VehicleId == vehicleId,
                cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                "Эта техника уже назначена топливозаправщиком.");
        }

        var fuelTruck = new FuelTruck(vehicleId);

        dbContext.Set<FuelTruck>()
            .Add(fuelTruck);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return fuelTruck.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user = await GetRequiredUserAsync();

        var fuelTruck = await dbContext
            .Set<FuelTruck>()
            .Include(x => x.Vehicle)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        await EnsureVehicleBelongsToUserScopeAsync(
            fuelTruck.Vehicle.BranchId);

        var vehicle = await dbContext.Vehicles
            .SingleOrDefaultAsync(
                x => x.Id == vehicleId &&
                     x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника не найдена или неактивна.");

        await EnsureVehicleBelongsToUserScopeAsync(
            vehicle.BranchId);

        var alreadyAssigned = await dbContext
            .Set<FuelTruck>()
            .AnyAsync(
                x => x.VehicleId == vehicleId &&
                     x.Id != id,
                cancellationToken);

        if (alreadyAssigned)
        {
            throw new InvalidOperationException(
                "Эта техника уже назначена другим топливозаправщиком.");
        }

        fuelTruck.ChangeVehicle(vehicleId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task SetOmnicommVehiclesAsync(
        Guid fuelTruckId,
        Guid? ussVehicleId,
        Guid? tankVehicleId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user = await GetRequiredUserAsync();

        var fuelTruck = await dbContext.FuelTrucks
            .Include(x => x.Vehicle)
            .SingleOrDefaultAsync(
                x => x.Id == fuelTruckId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        await EnsureVehicleBelongsToUserScopeAsync(
            fuelTruck.Vehicle.BranchId);

        await ValidateOmnicommVehicleAsync(
            ussVehicleId,
            "УСС",
            fuelTruck.Vehicle.BranchId,
            user,
            cancellationToken);

        await ValidateOmnicommVehicleAsync(
            tankVehicleId,
            "емкости",
            fuelTruck.Vehicle.BranchId,
            user,
            cancellationToken);

        fuelTruck.SetUssVehicle(
            ussVehicleId);

        fuelTruck.SetTankVehicle(
            tankVehicleId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(
            cancellationToken);

        var user = await GetRequiredUserAsync();

        var fuelTruck = await dbContext
            .Set<FuelTruck>()
            .Include(x => x.Vehicle)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        await EnsureVehicleBelongsToUserScopeAsync(
            fuelTruck.Vehicle.BranchId);

        var hasFuelingRecords = await dbContext
            .Set<FuelingRecord>()
            .AnyAsync(
                x => x.FuelTruckId == id,
                cancellationToken);

        if (hasFuelingRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить топливозаправщик, " +
                "поскольку с ним связаны записи заправок.");
        }

        dbContext.Set<FuelTruck>()
            .Remove(fuelTruck);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task ValidateOmnicommVehicleAsync(
        Guid? vehicleId,
        string purpose,
        Guid fuelTruckBranchId,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (!vehicleId.HasValue)
        {
            return;
        }

        var vehicle = await dbContext.Vehicles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == vehicleId.Value &&
                     x.IsActive,
                cancellationToken);

        if (vehicle is null)
        {
            throw new InvalidOperationException(
                $"Техника для назначения «{purpose}» " +
                "не найдена или неактивна.");
        }

        await EnsureVehicleBelongsToUserScopeAsync(
            vehicle.BranchId);

        if (vehicle.BranchId != fuelTruckBranchId)
        {
            throw new InvalidOperationException(
                $"Техника для назначения «{purpose}» " +
                "должна относиться к тому же филиалу.");
        }

        if (vehicle.OmnicommObjectId is null)
        {
            throw new InvalidOperationException(
                $"У выбранной техники для назначения «{purpose}» " +
                "не указан OmnicommObjectId.");
        }
    }

    private async Task EnsureVehicleBelongsToUserScopeAsync(
        Guid branchId)
    {
        await authorization.EnsureBranchAccessAsync(branchId);
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
}