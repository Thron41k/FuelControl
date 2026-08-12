using FuelControl.Domain.Entities;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IFuelTruckService
{
    Task<IReadOnlyList<FuelTruck>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> GetAvailableVehiclesAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}