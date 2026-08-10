using FuelControl.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FuelControl.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public string? TimeZoneId { get; set; }

    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }
}