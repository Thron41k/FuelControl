using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommVehicleService
{
    Task<IReadOnlyList<OmnicommObject>>
        GetVehiclesForBranchAsync(
            long omnicommGroupId,
            long actorId,
            CancellationToken cancellationToken = default);
}