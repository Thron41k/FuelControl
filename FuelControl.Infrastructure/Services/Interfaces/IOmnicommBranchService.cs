using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommBranchService
{
    Task<IReadOnlyList<OmnicommGroup>> GetBranchesAsync(
        CancellationToken cancellationToken = default);
}