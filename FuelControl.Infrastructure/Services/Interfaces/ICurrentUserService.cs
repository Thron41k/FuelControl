namespace FuelControl.Infrastructure.Services.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}