namespace FuelControl.Infrastructure.Services.Models;

public sealed class AdminUserModel
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public Guid? BranchId { get; init; }

    public string? BranchName { get; init; }

    public string Role { get; init; } = string.Empty;

    public bool IsLockedOut { get; init; }

    public bool EmailConfirmed { get; init; }
}