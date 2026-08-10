namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IFuelingRecordAuthorizationService
{
    Task<bool> CanCreateAsync(
        Guid fuelTruckId,
        CancellationToken cancellationToken = default);

    Task<bool> CanEditAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default);

    Task<bool> CanDeleteAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default);
}