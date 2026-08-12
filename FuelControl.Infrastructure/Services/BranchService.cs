using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class BranchService(
    FuelControlDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
    : IBranchService
{
    public async Task<IReadOnlyList<Branch>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Branch>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Branch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Branch>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        name = NormalizeName(name);

        var exists = await dbContext.Set<Branch>()
            .AnyAsync(
                x => x.Name == name,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "Филиал с таким названием уже существует.");
        }

        var branch = new Branch(name);

        dbContext.Set<Branch>().Add(branch);

        await dbContext.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }

    public async Task ImportFromOmnicommAsync(
        IReadOnlyList<OmnicommBranchImportModel> branches,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        foreach (var item in branches)
        {
            var name = NormalizeName(item.Name);

            var branch = await dbContext.Set<Branch>()
                .SingleOrDefaultAsync(
                    x => x.OmnicommId == item.OmnicommId,
                    cancellationToken);

            if (branch is not null)
            {
                if (branch.Name != name)
                {
                    branch.Rename(name);
                }

                continue;
            }

            var localBranch = await dbContext.Set<Branch>()
                .SingleOrDefaultAsync(
                    x => x.Name == name &&
                         x.OmnicommId == null,
                    cancellationToken);

            if (localBranch is not null)
            {
                localBranch.SetOmnicommId(item.OmnicommId);
                continue;
            }

            var newBranch = new Branch(name);

            newBranch.SetOmnicommId(item.OmnicommId);

            dbContext.Set<Branch>().Add(newBranch);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        name = NormalizeName(name);

        var branch = await dbContext.Set<Branch>()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Филиал не найден.");

        var exists = await dbContext.Set<Branch>()
            .AnyAsync(
                x => x.Id != id &&
                     x.Name == name,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "Филиал с таким названием уже существует.");
        }

        branch.Rename(name);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        var branch = await dbContext.Set<Branch>()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Филиал не найден.");

        var hasVehicles = await dbContext.Vehicles
            .AnyAsync(
                x => x.BranchId == id,
                cancellationToken);

        if (hasVehicles)
        {
            throw new InvalidOperationException(
                "Нельзя удалить филиал, к которому привязана техника.");
        }

        var hasFuelTrucks = await dbContext.FuelTrucks
            .AnyAsync(
                x => x.Vehicle.BranchId == id,
                cancellationToken);

        if (hasFuelTrucks)
        {
            throw new InvalidOperationException(
                "Нельзя удалить филиал, к которому привязаны АТЗ.");
        }

        var hasOperators = await dbContext.Operators
            .AnyAsync(
                x => x.BranchId == id,
                cancellationToken);

        if (hasOperators)
        {
            throw new InvalidOperationException(
                "Нельзя удалить филиал, к которому привязаны машинисты.");
        }

        var hasUsers = await userManager.Users
            .AnyAsync(
                x => x.BranchId == id,
                cancellationToken);

        if (hasUsers)
        {
            throw new InvalidOperationException(
                "Нельзя удалить филиал, к которому привязаны пользователи.");
        }

        dbContext.Set<Branch>().Remove(branch);

        await dbContext.SaveChangesAsync(cancellationToken);
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
                "Недостаточно прав для выполнения операции.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Название филиала не может быть пустым.",
                nameof(name));
        }

        return name.Trim();
    }

    public async Task<IReadOnlyList<Branch>> GetAvailableForCurrentUserAsync(
        CancellationToken cancellationToken = default)
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

        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            return await dbContext.Set<Branch>()
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        if (await userManager.IsInRoleAsync(
                user,
                Roles.Dispatcher))
        {
            if (user.BranchId is null)
            {
                return [];
            }

            return await dbContext.Set<Branch>()
                .AsNoTracking()
                .Where(x => x.Id == user.BranchId.Value)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        throw new UnauthorizedAccessException(
            "Недостаточно прав для просмотра филиалов.");
    }
}