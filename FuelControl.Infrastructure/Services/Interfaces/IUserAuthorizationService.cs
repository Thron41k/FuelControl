namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IUserAuthorizationService
{
    Task EnsureAuthenticatedAsync(
        CancellationToken cancellationToken = default);

    Task EnsureDispatcherAsync(
        CancellationToken cancellationToken = default);

    Task EnsureModeratorAsync(
        CancellationToken cancellationToken = default);

    Task EnsureAdminAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsInRoleAsync(
        string role,
        CancellationToken cancellationToken = default);

    Task<bool> IsAdminAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> GetCurrentUserIdAsync(
        CancellationToken cancellationToken = default);

    Task<Guid?> GetCurrentUserBranchIdAsync(
        CancellationToken cancellationToken = default);

    Task EnsureBranchAccessAsync(
        Guid branchId,
        CancellationToken cancellationToken = default);
}