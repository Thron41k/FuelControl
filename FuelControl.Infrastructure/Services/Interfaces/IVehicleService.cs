using FuelControl.Domain.Entities;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IVehicleService
{
    Task<IReadOnlyList<Vehicle>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(string name,
        string registrationNumber,
        Guid? branchId,
        string? inventoryNumber,
        long? omnicommObjectId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string name,
        string registrationNumber,
        Guid? branchId,
        string? inventoryNumber,
        long? omnicommObjectId,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<long>> GetExistingOmnicommVehicleIdsAsync(
        CancellationToken cancellationToken = default);
}