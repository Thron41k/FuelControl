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
    ICurrentUserService currentUserService)
    : IFuelTruckService
{
    public async Task<IReadOnlyList<FuelTruck>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        return await dbContext.Set<FuelTruck>()
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .ThenInclude(x => x.Branch)
            .Include(x => x.UssVehicle)
            .Include(x => x.TankVehicle)
            .OrderBy(x => x.Vehicle.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> GetAvailableVehiclesAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        return await dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.IsActive &&
                !dbContext.Set<FuelTruck>()
                    .Any(
                        x => x.VehicleId == vehicle.Id))
            .Include(x => x.Branch)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        var vehicleExists = await dbContext.Vehicles
            .AnyAsync(
                x => x.Id == vehicleId &&
                     x.IsActive,
                cancellationToken);

        if (!vehicleExists)
        {
            throw new InvalidOperationException(
                "Техника не найдена или неактивна.");
        }

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

        var fuelTruckVehicle = new FuelTruck(
            vehicleId);

        dbContext.Set<FuelTruck>()
            .Add(fuelTruckVehicle);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return fuelTruckVehicle.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        var fuelTruckVehicle = await dbContext
            .Set<FuelTruck>()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        var vehicleExists = await dbContext.Vehicles
            .AnyAsync(
                x => x.Id == vehicleId &&
                     x.IsActive,
                cancellationToken);

        if (!vehicleExists)
        {
            throw new InvalidOperationException(
                "Техника не найдена или неактивна.");
        }

        var alreadyAssigned = await dbContext
            .Set<FuelTruck>()
            .AnyAsync(
                x => x.VehicleId == vehicleId &&
                     x.Id != id,
                cancellationToken);

        if (alreadyAssigned)
        {
            throw new InvalidOperationException(
                "Эта техника уже назначена другим " +
                "топливозаправщиком.");
        }

        fuelTruckVehicle.ChangeVehicle(vehicleId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        var fuelTruckVehicle = await dbContext
            .Set<FuelTruck>()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");
        var hasFuelingRecords = await dbContext
            .Set<FuelingRecord>()
            .AnyAsync(
                x => x.VehicleId == fuelTruckVehicle.VehicleId,
                cancellationToken);

        if (hasFuelingRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить топливозаправщик, " +
                "поскольку с этой техникой связаны записи заправок.");
        }
        dbContext.Set<FuelTruck>()
            .Remove(fuelTruckVehicle);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task EnsureAdminAsync()
    {
        if (currentUserService.UserId is not { } userId)
        {
            throw new UnauthorizedAccessException(
                "Пользователь не авторизован.");
        }

        var user = await userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Пользователь не найден.");
        }

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            throw new UnauthorizedAccessException(
                "Только администратор может управлять " +
                "топливозаправщиками.");
        }
    }

    public async Task SetOmnicommVehiclesAsync(
        Guid fuelTruckId,
        Guid? ussVehicleId,
        Guid? tankVehicleId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        var fuelTruck =
            await dbContext.FuelTrucks
                .SingleOrDefaultAsync(
                    x => x.Id == fuelTruckId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        await ValidateOmnicommVehicleAsync(
            ussVehicleId,
            "УСС",
            cancellationToken);

        await ValidateOmnicommVehicleAsync(
            tankVehicleId,
            "емкости",
            cancellationToken);

        fuelTruck.SetUssVehicle(
            ussVehicleId);

        fuelTruck.SetTankVehicle(
            tankVehicleId);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task ValidateOmnicommVehicleAsync(
        Guid? vehicleId,
        string purpose,
        CancellationToken cancellationToken)
    {
        if (!vehicleId.HasValue)
            return;

        var vehicle =
            await dbContext.Vehicles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == vehicleId.Value &&
                         x.IsActive,
                    cancellationToken);

        if (vehicle is null)
        {
            throw new InvalidOperationException(
                $"Техника для назначения «{purpose}» не найдена " +
                "или неактивна.");
        }

        if (vehicle.OmnicommObjectId is null)
        {
            throw new InvalidOperationException(
                $"У выбранной техники для назначения «{purpose}» " +
                "не указан OmnicommObjectId.");
        }
    }
}