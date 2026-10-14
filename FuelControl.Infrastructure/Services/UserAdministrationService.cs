using FuelControl.Infrastructure.Identity;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class UserAdministrationService(
    FuelControlDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IUserAuthorizationService authorization,
    ICurrentUserService currentUserService)
    : IUserAdministrationService
{
    public async Task<IReadOnlyList<AdminUserModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAdminAsync(cancellationToken);

        var users = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.AdditionalBranchAccesses)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);

        var result = new List<AdminUserModel>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(MapUser(user, roles));
        }

        return result;
    }

    public async Task<IReadOnlyList<AdminUserModel>> GetForBranchAccessAsync(
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(cancellationToken);

        var isAdmin = await authorization.IsAdminAsync(cancellationToken);
        var query = dbContext.Users
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.AdditionalBranchAccesses)
            .AsQueryable();

        if (!isAdmin)
        {
            var moderatorBranchId =
                await authorization.GetCurrentUserBranchIdAsync(cancellationToken);

            if (moderatorBranchId is null)
            {
                return [];
            }

            query = query.Where(x => x.BranchId == moderatorBranchId.Value);
        }

        var users = await query
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);

        var result = new List<AdminUserModel>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);

            if (!isAdmin && !IsDispatcherOnly(roles))
            {
                continue;
            }

            result.Add(MapUser(user, roles));
        }

        return result;
    }

    public async Task<Guid> CreateAsync(
        string email,
        string displayName,
        string password,
        Guid? branchId,
        string role,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAdminAsync(cancellationToken);

        var normalizedEmail = NormalizeEmail(email);
        ValidateRoleAndBranch(role, branchId);
        await EnsureBranchExistsAsync(branchId, cancellationToken);

        var existing = await userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                "Пользователь с таким Email уже существует.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            DisplayName = NormalizeDisplayName(displayName),
            BranchId = branchId,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        EnsureSucceeded(result, "Не удалось создать пользователя.");

        result = await userManager.AddToRoleAsync(user, role);
        EnsureSucceeded(result, "Не удалось назначить роль пользователю.");

        return user.Id;
    }

    public async Task UpdateAsync(
        Guid userId,
        string email,
        string displayName,
        Guid? branchId,
        string role,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAdminAsync(cancellationToken);

        ValidateRoleAndBranch(role, branchId);
        await EnsureBranchExistsAsync(branchId, cancellationToken);

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("Пользователь не найден.");
        }

        var normalizedEmail = NormalizeEmail(email);
        var existing = await userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null && existing.Id != userId)
        {
            throw new InvalidOperationException(
                "Пользователь с таким Email уже существует.");
        }

        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.DisplayName = NormalizeDisplayName(displayName);
        user.BranchId = branchId;

        var result = await userManager.UpdateAsync(user);
        EnsureSucceeded(result, "Не удалось обновить пользователя.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count != 1 || !currentRoles.Contains(role))
        {
            if (currentRoles.Count > 0)
            {
                result = await userManager.RemoveFromRolesAsync(user, currentRoles);
                EnsureSucceeded(result, "Не удалось изменить текущую роль.");
            }

            result = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(result, "Не удалось назначить новую роль.");
        }

        // Основной филиал не должен одновременно храниться как дополнительный.
        if (branchId.HasValue)
        {
            var duplicateAccesses = await dbContext.UserBranchAccesses
                .Where(x => x.UserId == userId && x.BranchId == branchId.Value)
                .ToListAsync(cancellationToken);

            if (duplicateAccesses.Count > 0)
            {
                dbContext.UserBranchAccesses.RemoveRange(duplicateAccesses);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task SetAdditionalBranchesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> branchIds,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureModeratorAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(branchIds);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Не указан пользователь.", nameof(userId));
        }

        var isAdmin = await authorization.IsAdminAsync(cancellationToken);
        var targetUser = await dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        var requestedBranchIds = branchIds.Distinct().ToHashSet();
        if (requestedBranchIds.Contains(Guid.Empty))
        {
            throw new ArgumentException(
                "Список содержит некорректный идентификатор филиала.",
                nameof(branchIds));
        }

        // Основной филиал исключается из набора дополнительных назначений.
        if (targetUser.BranchId.HasValue)
        {
            requestedBranchIds.Remove(targetUser.BranchId.Value);
        }

        IReadOnlyCollection<Guid> moderatorAccessibleIds = [];
        HashSet<Guid>? moderatorAccessibleSet = null;

        if (!isAdmin)
        {
            await EnsureModeratorCanManageUserAsync(targetUser, cancellationToken);

            moderatorAccessibleIds =
                await authorization.GetAccessibleBranchIdsAsync(cancellationToken);
            moderatorAccessibleSet = moderatorAccessibleIds.ToHashSet();

            var forbiddenBranchIds = requestedBranchIds
                .Where(id => !moderatorAccessibleSet.Contains(id))
                .ToArray();

            if (forbiddenBranchIds.Length > 0)
            {
                throw new UnauthorizedAccessException(
                    "Нельзя назначить филиалы, к которым у вас нет доступа. " +
                    $"ID: {string.Join(", ", forbiddenBranchIds)}.");
            }
        }

        // Проверяем, что все переданные филиалы существуют.
        if (requestedBranchIds.Count > 0)
        {
            var existingIds = await dbContext.Branchs
                .AsNoTracking()
                .Where(x => requestedBranchIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var existingSet = existingIds.ToHashSet();
            var missingIds = requestedBranchIds
                .Where(id => !existingSet.Contains(id))
                .ToArray();

            if (missingIds.Length > 0)
            {
                throw new InvalidOperationException(
                    "Некоторые филиалы не найдены. " +
                    $"ID: {string.Join(", ", missingIds)}.");
            }
        }

        var currentAccesses = await dbContext.UserBranchAccesses
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var currentBranchIds = currentAccesses
            .Select(x => x.BranchId)
            .ToHashSet();

        // Moderator изменяет назначения только внутри своего набора доступных
        // филиалов. Имеющиеся назначения за пределами этого набора сохраняются.
        var effectiveRequestedIds = requestedBranchIds;
        if (!isAdmin && moderatorAccessibleSet is not null)
        {
            effectiveRequestedIds = currentBranchIds
                .Where(id => !moderatorAccessibleSet.Contains(id))
                .Concat(requestedBranchIds)
                .ToHashSet();
        }

        var accessesToRemove = currentAccesses
            .Where(x => !effectiveRequestedIds.Contains(x.BranchId))
            .ToList();

        if (accessesToRemove.Count > 0)
        {
            dbContext.UserBranchAccesses.RemoveRange(accessesToRemove);
        }

        foreach (var branchId in effectiveRequestedIds)
        {
            if (!currentBranchIds.Contains(branchId))
            {
                dbContext.UserBranchAccesses.Add(
                    new UserBranchAccess(userId, branchId));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAdminAsync(cancellationToken);

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("Пользователь не найден.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        EnsureSucceeded(result, "Не удалось изменить пароль.");
    }

    public async Task SetLockedAsync(
        Guid userId,
        bool locked,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAdminAsync(cancellationToken);

        if (currentUserService.UserId == userId)
        {
            throw new InvalidOperationException(
                "Нельзя заблокировать текущего пользователя.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("Пользователь не найден.");
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = locked
            ? DateTimeOffset.UtcNow.AddYears(100)
            : null;

        var result = await userManager.UpdateAsync(user);
        EnsureSucceeded(
            result,
            locked
                ? "Не удалось заблокировать пользователя."
                : "Не удалось разблокировать пользователя.");
    }

    private async Task EnsureModeratorCanManageUserAsync(
        ApplicationUser targetUser,
        CancellationToken cancellationToken)
    {
        var moderatorBranchId =
            await authorization.GetCurrentUserBranchIdAsync(cancellationToken);

        if (moderatorBranchId is null || targetUser.BranchId != moderatorBranchId)
        {
            throw new UnauthorizedAccessException(
                "Moderator может управлять назначениями только пользователей " +
                "с таким же основным филиалом.");
        }

        var roles = await userManager.GetRolesAsync(targetUser);
        if (!IsDispatcherOnly(roles))
        {
            throw new UnauthorizedAccessException(
                "Moderator может изменять дополнительные филиалы только " +
                "для пользователей с ролью Dispatcher.");
        }
    }

    private static bool IsDispatcherOnly(IList<string> roles) =>
        roles.Count == 1 && roles.Contains(Roles.Dispatcher);

    private static AdminUserModel MapUser(
        ApplicationUser user,
        IList<string> roles)
    {
        return new AdminUserModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            BranchId = user.BranchId,
            BranchName = user.Branch?.Name,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsLockedOut = user.LockoutEnd.HasValue &&
                          user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            EmailConfirmed = user.EmailConfirmed,
            AdditionalBranchIds = user.AdditionalBranchAccesses
                .Select(x => x.BranchId)
                .ToArray()
        };
    }

    private static void ValidateRoleAndBranch(string role, Guid? branchId)
    {
        if (!Roles.All.Contains(role, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Недопустимая роль: {role}");
        }

        if (role != Roles.Admin && branchId is null)
        {
            throw new InvalidOperationException(
                "Для диспетчера и модератора необходимо указать филиал.");
        }
    }

    private async Task EnsureBranchExistsAsync(
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (!branchId.HasValue)
        {
            return;
        }

        var exists = await dbContext.Branchs
            .AnyAsync(x => x.Id == branchId.Value, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Указанный филиал не найден.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email обязателен.", nameof(email));
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Необходимо указать имя пользователя.",
                nameof(displayName));
        }

        return displayName.Trim();
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(x => x.Description));
        throw new InvalidOperationException($"{message} {errors}");
    }
}
