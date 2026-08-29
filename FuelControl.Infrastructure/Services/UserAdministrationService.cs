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
    RoleManager<IdentityRole<Guid>> roleManager,
    ICurrentUserService currentUserService)
    : IUserAdministrationService
{
    public async Task<IReadOnlyList<AdminUserModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users =
            await dbContext.Users
                .AsNoTracking()
                .Include(x => x.Branch)
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Email)
                .ToListAsync(cancellationToken);

        var result =
            new List<AdminUserModel>(
                users.Count);

        foreach (var user in users)
        {
            var roles =
                await userManager.GetRolesAsync(user);

            result.Add(
                new AdminUserModel
                {
                    Id = user.Id,

                    Email =
                        user.Email ?? string.Empty,

                    DisplayName =
                        user.DisplayName,

                    BranchId =
                        user.BranchId,

                    BranchName =
                        user.Branch?.Name,

                    Role =
                        roles.FirstOrDefault()
                        ?? string.Empty,

                    IsLockedOut =
                        user.LockoutEnd.HasValue &&
                        user.LockoutEnd.Value > DateTimeOffset.UtcNow,

                    EmailConfirmed =
                        user.EmailConfirmed
                });
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
        await EnsureAdminAsync();

        var normalizedEmail =
            NormalizeEmail(email);

        ValidateRole(role);

        await EnsureBranchExistsAsync(
            branchId,
            cancellationToken);

        var existing =
            await userManager.FindByEmailAsync(
                normalizedEmail);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                "Пользователь с таким Email уже существует.");
        }

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),

                Email =
                    normalizedEmail,

                UserName =
                    normalizedEmail,

                DisplayName =
                    displayName.Trim(),

                BranchId =
                    branchId,

                EmailConfirmed = true
            };

        var result =
            await userManager.CreateAsync(
                user,
                password);

        EnsureSucceeded(
            result,
            "Не удалось создать пользователя.");

        result =
            await userManager.AddToRoleAsync(
                user,
                role);

        EnsureSucceeded(
            result,
            "Не удалось назначить роль пользователю.");

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
        await EnsureAdminAsync();

        ValidateRole(role);

        await EnsureBranchExistsAsync(
            branchId,
            cancellationToken);

        var user =
            await userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
        {
            throw new InvalidOperationException(
                "Пользователь не найден.");
        }

        var normalizedEmail =
            NormalizeEmail(email);

        var existing =
            await userManager.FindByEmailAsync(
                normalizedEmail);

        if (existing is not null &&
            existing.Id != userId)
        {
            throw new InvalidOperationException(
                "Пользователь с таким Email уже существует.");
        }

        user.Email =
            normalizedEmail;

        user.UserName =
            normalizedEmail;

        user.DisplayName =
            displayName.Trim();

        user.BranchId =
            branchId;

        var result =
            await userManager.UpdateAsync(
                user);

        EnsureSucceeded(
            result,
            "Не удалось обновить пользователя.");

        var currentRoles =
            await userManager.GetRolesAsync(user);

        if (!currentRoles.Contains(role))
        {
            if (currentRoles.Count > 0)
            {
                result =
                    await userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles);

                EnsureSucceeded(
                    result,
                    "Не удалось изменить текущую роль.");
            }

            result =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            EnsureSucceeded(
                result,
                "Не удалось назначить новую роль.");
        }
    }

    public async Task ResetPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        var user =
            await userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
        {
            throw new InvalidOperationException(
                "Пользователь не найден.");
        }

        var token =
            await userManager.GeneratePasswordResetTokenAsync(
                user);

        var result =
            await userManager.ResetPasswordAsync(
                user,
                token,
                password);

        EnsureSucceeded(
            result,
            "Не удалось изменить пароль.");
    }

    public async Task SetLockedAsync(
        Guid userId,
        bool locked,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync();

        if (currentUserService.UserId == userId)
        {
            throw new InvalidOperationException(
                "Нельзя заблокировать текущего пользователя.");
        }

        var user =
            await userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
        {
            throw new InvalidOperationException(
                "Пользователь не найден.");
        }

        user.LockoutEnabled = true;

        user.LockoutEnd =
            locked
                ? DateTimeOffset.UtcNow.AddYears(100)
                : null;

        var result =
            await userManager.UpdateAsync(
                user);

        EnsureSucceeded(
            result,
            locked
                ? "Не удалось заблокировать пользователя."
                : "Не удалось разблокировать пользователя.");
    }

    private async Task EnsureAdminAsync()
    {
        if (!currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        var currentUserId =
            currentUserService.UserId;

        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        var user =
            await userManager.FindByIdAsync(
                currentUserId.Value.ToString());

        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (!await userManager.IsInRoleAsync(
                user,
                Roles.Admin))
        {
            throw new UnauthorizedAccessException();
        }
    }

    private static void ValidateRole(
        string role)
    {
        if (!Roles.All.Contains(
                role,
                StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Недопустимая роль: {role}");
        }
    }

    private async Task EnsureBranchExistsAsync(
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (!branchId.HasValue)
            return;

        var exists =
            await dbContext.Branchs
                .AnyAsync(
                    x => x.Id == branchId.Value,
                    cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Указанный филиал не найден.");
        }
    }

    private static string NormalizeEmail(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email обязателен.",
                nameof(email));
        }

        return email
            .Trim()
            .ToLowerInvariant();
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string message)
    {
        if (result.Succeeded)
            return;

        var errors =
            string.Join(
                "; ",
                result.Errors.Select(
                    x => x.Description));

        throw new InvalidOperationException(
            $"{message} {errors}");
    }
}