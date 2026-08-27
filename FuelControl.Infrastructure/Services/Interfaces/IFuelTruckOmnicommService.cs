using FuelControl.Domain.Entities;
using FuelControl.Domain.Enums;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IFuelTruckOmnicommService
{
    Task<IReadOnlyList<FuelTruckOmnicommBinding>> GetAllAsync(
        Guid fuelTruckId,
        CancellationToken cancellationToken = default);

    Task<long?> GetObjectIdAsync(
        Guid fuelTruckId,
        FuelTruckOmnicommPurpose purpose,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        Guid fuelTruckId,
        long omnicommObjectId,
        FuelTruckOmnicommPurpose purpose,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid fuelTruckId,
        FuelTruckOmnicommPurpose purpose,
        CancellationToken cancellationToken = default);
}