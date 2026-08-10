using FuelControl.Application.Authorization;
using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Authorization;

public sealed class FuelingRecordAuthorizationService(
    FuelControlDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
    : IFuelingRecordAuthorizationService
{
    public async Task<bool> CanCreateAsync(
        Guid fuelTruckId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
            return false;

        // Администратор имеет полный доступ.
        if (await userManager.IsInRoleAsync(user, "Admin"))
            return true;

        // Только диспетчер может работать с ведомостями.
        if (!await userManager.IsInRoleAsync(user, "Dispatcher"))
            return false;

        // У диспетчера должен быть назначен филиал.
        if (user.BranchId is null)
            return false;

        // Получаем филиал АТЗ.
        var fuelTruckBranchId = await dbContext.FuelTrucks
            .Where(x => x.Id == fuelTruckId)
            .Select(x => (Guid?)x.BranchId)
            .SingleOrDefaultAsync(cancellationToken);

        if (fuelTruckBranchId is null)
            return false;

        // Главная проверка.
        return fuelTruckBranchId == user.BranchId;
    }

    public async Task<bool> CanEditAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
            return false;

        // Администратор имеет полный доступ.
        if (await userManager.IsInRoleAsync(user, "Admin"))
            return true;

        // Только диспетчер может редактировать ведомости.
        if (!await userManager.IsInRoleAsync(user, "Dispatcher"))
            return false;

        if (user.BranchId is null)
            return false;

        // Получаем филиал АТЗ через FuelingRecord.
        var fuelTruckBranchId = await dbContext.FuelingRecords
            .Where(x => x.Id == fuelingRecordId)
            .Select(x => (Guid?)x.FuelTruck.BranchId)
            .SingleOrDefaultAsync(cancellationToken);

        if (fuelTruckBranchId is null)
            return false;

        return fuelTruckBranchId == user.BranchId;
    }

    public async Task<bool> CanDeleteAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default)
    {
        return await CanEditAsync(
            fuelingRecordId,
            cancellationToken);
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (string.IsNullOrWhiteSpace(currentUserService.UserId))
            return null;

        return await userManager.FindByIdAsync(
            currentUserService.UserId);
    }
}