using FuelControl.Domain.Entities;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOperatorService
{
    Task<IReadOnlyList<Operator>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        string fullName,
        Guid? branchId,
        string? personnelNumber,
        string? rfidTagId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string fullName,
        Guid? branchId,
        string? personnelNumber,
        string? rfidTagId,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}