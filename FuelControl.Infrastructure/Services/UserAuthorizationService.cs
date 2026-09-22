using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FuelControl.Infrastructure.Services;

public sealed class UserAuthorizationService(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
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
        var user =
            await GetCurrentUserAsync(
                cancellationToken);

        if (await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            return;
        }

        if (user.BranchId is null)
        {
            throw new UnauthorizedAccessException(
                "Для пользователя не назначен филиал.");
        }

        if (user.BranchId.Value != branchId)
        {
            throw new UnauthorizedAccessException(
                "Объект относится к другому филиалу.");
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
}