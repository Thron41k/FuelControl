using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>IANA timezone, например Asia/Chita. Если пусто — берём из браузера.</summary>
    public string? TimeZoneId { get; set; }

    public Guid? BranchId { get; set; }
}
