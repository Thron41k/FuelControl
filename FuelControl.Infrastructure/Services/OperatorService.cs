
using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class OperatorService(
    FuelControlDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
    : IOperatorService
{
    public async Task<IReadOnlyList<Operator>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var query = dbContext.Operators
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
                .ThenBy(x => x.FullName)
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
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        string fullName,
        Guid? branchId,
        string? personnelNumber,
        string? rfidTagId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var targetBranchId = await ResolveBranchForWriteAsync(
            user,
            branchId);

        var normalizedFullName = NormalizeFullName(fullName);
        var normalizedRfidTagId =
            NormalizeRfidTagId(rfidTagId);
        var normalizedPersonnelNumber =
            NormalizePersonnelNumber(personnelNumber);

        await EnsureRfidTagIsAvailableAsync(
            normalizedRfidTagId,
            null,
            cancellationToken);

        await EnsureFullNameIsAvailableAsync(
            normalizedFullName,
            targetBranchId,
            null,
            cancellationToken);

        var @operator = new Operator(
            normalizedFullName,
            targetBranchId,
            normalizedPersonnelNumber,
            normalizedRfidTagId);

        dbContext.Operators.Add(@operator);

        await dbContext.SaveChangesAsync(cancellationToken);

        return @operator.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string fullName,
        Guid? branchId,
        string? personnelNumber,
        string? rfidTagId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var @operator = await dbContext.Operators
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Водитель не найден.");

        var isAdmin = await userManager.IsInRoleAsync(
            user,
            Roles.Admin);

        Guid targetBranchId;

        if (isAdmin)
        {
            if (branchId is null)
            {
                throw new ArgumentException(
                    "Необходимо указать филиал.",
                    nameof(branchId));
            }

            targetBranchId = branchId.Value;
        }
        else
        {
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

            if (@operator.BranchId != user.BranchId.Value)
            {
                throw new UnauthorizedAccessException(
                    "Водитель относится к другому филиалу.");
            }

            // Никогда не доверяем branchId,
            // пришедшему от Dispatcher из браузера.
            targetBranchId = user.BranchId.Value;
        }
        var normalizedRfidTagId =
            NormalizeRfidTagId(rfidTagId);
        var normalizedFullName = NormalizeFullName(fullName);
        var normalizedPersonnelNumber =
            NormalizePersonnelNumber(personnelNumber);

        await EnsureFullNameIsAvailableAsync(
            normalizedFullName,
            targetBranchId,
            id,
            cancellationToken);

        await EnsureRfidTagIsAvailableAsync(
            normalizedRfidTagId,
            id,
            cancellationToken);

        @operator.Update(
            normalizedFullName,
            targetBranchId,
            normalizedPersonnelNumber,
            normalizedRfidTagId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        var @operator = await dbContext.Operators
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Водитель не найден.");

        var isAdmin = await userManager.IsInRoleAsync(
            user,
            Roles.Admin);

        if (isAdmin)
        {
            SetOperatorState(@operator, isActive);

            await dbContext.SaveChangesAsync(
                cancellationToken);

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

        if (@operator.BranchId != user.BranchId.Value)
        {
            throw new UnauthorizedAccessException(
                "Водитель относится к другому филиалу.");
        }

        SetOperatorState(@operator, isActive);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync();

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            throw new UnauthorizedAccessException(
                "Только администратор может удалить водителя.");
        }

        var @operator = await dbContext.Operators
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Водитель не найден.");

        if (@operator.IsActive)
        {
            throw new InvalidOperationException(
                "Активного водителя нельзя удалить. " +
                "Сначала деактивируйте его.");
        }

        var hasFuelingRecords = await dbContext
            .Set<FuelingRecord>()
            .AnyAsync(
                x => x.OperatorId == id,
                cancellationToken);

        if (hasFuelingRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить водителя, " +
                "поскольку с ним связаны записи заправок.");
        }

        dbContext.Operators.Remove(@operator);

        await dbContext.SaveChangesAsync(
            cancellationToken);
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

        // requestedBranchId намеренно игнорируется.
        return user.BranchId.Value;
    }

    private async Task EnsureFullNameIsAvailableAsync(
        string fullName,
        Guid branchId,
        Guid? excludedOperatorId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Operators
            .AnyAsync(
                x =>
                    x.BranchId == branchId &&
                    x.FullName == fullName &&
                    (excludedOperatorId == null ||
                     x.Id != excludedOperatorId.Value),
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "В этом филиале уже существует водитель " +
                "с таким ФИО.");
        }
    }

    private static void SetOperatorState(
        Operator @operator,
        bool isActive)
    {
        if (isActive)
        {
            @operator.Activate();
        }
        else
        {
            @operator.Deactivate();
        }
    }

    private static string NormalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Необходимо указать ФИО водителя.",
                nameof(fullName));
        }

        return fullName.Trim();
    }

    private static string? NormalizePersonnelNumber(
        string? personnelNumber)
    {
        return string.IsNullOrWhiteSpace(personnelNumber)
            ? null
            : personnelNumber.Trim();
    }

    private async Task EnsureRfidTagIsAvailableAsync(
        string? rfidTagId,
        Guid? excludedOperatorId,
        CancellationToken cancellationToken)
    {
        if (rfidTagId is null)
            return;

        var operatorExists =
            await dbContext.Operators.AnyAsync(
                x =>
                    x.RfidTagId == rfidTagId &&
                    (excludedOperatorId == null ||
                     x.Id != excludedOperatorId.Value),
                cancellationToken);

        if (operatorExists)
        {
            throw new InvalidOperationException(
                "RFID метка уже назначена другому водителю.");
        }

        var vehicleExists =
            await dbContext.Vehicles.AnyAsync(
                x => x.RfidTagId == rfidTagId,
                cancellationToken);

        if (vehicleExists)
        {
            throw new InvalidOperationException(
                "RFID метка уже назначена технике.");
        }
    }

    private static string? NormalizeRfidTagId(
        string? rfidTagId)
    {
        if (string.IsNullOrWhiteSpace(rfidTagId))
            return null;

        return rfidTagId
            .Trim()
            .ToUpperInvariant();
    }
}

