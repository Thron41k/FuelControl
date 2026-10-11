using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class UserAuthorizationService(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService,
    FuelControlDbContext dbContext)
    : IUserAuthorizationService
{
    public async Task EnsureAuthenticatedAsync(
        CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(cancellationToken);
    }

    public async Task EnsureDispatcherAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureAnyRoleAsync(
            [
                Roles.Dispatcher,
                Roles.Moderator,
                Roles.Admin
            ],
            cancellationToken);
    }

    public async Task EnsureModeratorAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureAnyRoleAsync(
            [
                Roles.Moderator,
                Roles.Admin
            ],
            cancellationToken);
    }

    public async Task EnsureAdminAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureAnyRoleAsync(
            [
                Roles.Admin
            ],
            cancellationToken);
    }

    public async Task<bool> IsInRoleAsync(
        string role,
        CancellationToken cancellationToken = default)
    {
        var user =
            await GetCurrentUserAsync(
                cancellationToken);

        return await userManager.IsInRoleAsync(
            user,
            role);
    }

    public async Task<bool> IsAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var user =
            await GetCurrentUserAsync(
                cancellationToken);

        return await userManager.IsInRoleAsync(
            user,
            Roles.Admin);
    }

    public async Task<Guid> GetCurrentUserIdAsync(
        CancellationToken cancellationToken = default)
    {
        var user =
            await GetCurrentUserAsync(
                cancellationToken);

        return user.Id;
    }

    public async Task<Guid?> GetCurrentUserBranchIdAsync(
        CancellationToken cancellationToken = default)
    {
        var user =
            await GetCurrentUserAsync(
                cancellationToken);

        return user.BranchId;
    }

    public async Task EnsureBranchAccessAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDispatcherAsync(
            cancellationToken);

        var user = await GetCurrentUserAsync(
            cancellationToken);

        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            return;
        }

        // Основной филиал.
        if (user.BranchId == branchId)
        {
            return;
        }

        // Дополнительный филиал.
        var hasAccess = await dbContext.UserBranchAccesses
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == user.Id &&
                     x.BranchId == branchId,
                cancellationToken);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "У пользователя нет доступа к этому филиалу.");
        }
    }

    private async Task EnsureAnyRoleAsync(
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var user =
            await GetCurrentUserAsync(
                cancellationToken);

        foreach (var role in roles)
        {
            if (await userManager.IsInRoleAsync(
                    user,
                    role))
            {
                return;
            }
        }

        throw new UnauthorizedAccessException(
            "Недостаточно прав для выполнения операции.");
    }

    private async Task<ApplicationUser> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated ||
            currentUserService.UserId is not { } userId)
        {
            throw new UnauthorizedAccessException(
                "Пользователь не авторизован.");
        }

        return await userManager.FindByIdAsync(
                   userId.ToString())
               ?? throw new UnauthorizedAccessException(
                   "Текущий пользователь не найден.");
    }

    public async Task<IReadOnlyCollection<Guid>>
        GetAccessibleBranchIdsAsync(
            CancellationToken cancellationToken = default)
    {
        await EnsureDispatcherAsync(
            cancellationToken);

        var user = await GetCurrentUserAsync(
            cancellationToken);

        // Администратор работает со всеми филиалами.
        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            return await dbContext.Branchs
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        var branchIds = await dbContext.UserBranchAccesses
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Select(x => x.BranchId)
            .ToListAsync(cancellationToken);

        // Основной филиал всегда входит в список доступа.
        if (user.BranchId is { } primaryBranchId)
        {
            branchIds.Add(primaryBranchId);
        }

        return branchIds
            .Distinct()
            .ToArray();
    }
}