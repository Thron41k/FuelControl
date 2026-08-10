namespace FuelControl.Infrastructure.Services.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }

    bool IsAuthenticated { get; }
}