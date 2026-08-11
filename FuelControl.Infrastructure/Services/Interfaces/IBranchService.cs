using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Services.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IBranchService
{
    Task<IReadOnlyList<Branch>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Branch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task ImportFromOmnicommAsync(
        IReadOnlyList<OmnicommBranchImportModel> branches,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Branch>> GetAvailableForCurrentUserAsync(
        CancellationToken cancellationToken = default);
}