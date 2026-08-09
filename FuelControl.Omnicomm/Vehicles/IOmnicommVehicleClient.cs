using FuelControl.Omnicomm.Models;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles;

public interface IOmnicommVehicleClient
{
    Task<OmnicommVehiclesTreeResponse> GetVehiclesTreeAsync(
        long parentGroupId,
        long actorId,
        CancellationToken cancellationToken = default);
}