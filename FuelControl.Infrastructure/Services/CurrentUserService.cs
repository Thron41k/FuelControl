using System.Security.Claims;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FuelControl.Infrastructure.Services;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private ClaimsPrincipal? User =>
        httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier);
}