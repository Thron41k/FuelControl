using FuelControl.Domain.Entities;

namespace FuelControl.Infrastructure.Identity;

public sealed class UserBranchAccess
{
    private UserBranchAccess()
    {
    }

    public UserBranchAccess(
        Guid userId,
        Guid branchId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан пользователь.",
                nameof(userId));
        }

        if (branchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан филиал.",
                nameof(branchId));
        }

        UserId = userId;
        BranchId = branchId;
    }

    public Guid UserId { get; private set; }

    public Guid BranchId { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    public Branch Branch { get; private set; } = null!;
}