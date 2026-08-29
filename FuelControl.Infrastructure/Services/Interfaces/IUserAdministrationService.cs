using FuelControl.Infrastructure.Services.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<AdminUserModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        string email,
        string displayName,
        string password,
        Guid? branchId,
        string role,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid userId,
        string email,
        string displayName,
        Guid? branchId,
        string role,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default);

    Task SetLockedAsync(
        Guid userId,
        bool locked,
        CancellationToken cancellationToken = default);
}