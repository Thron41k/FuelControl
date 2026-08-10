using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles;

public interface IOmnicommVehicleClient
{
    Task<OmnicommVehiclesTreeResponse> GetVehiclesTreeAsync(
        long parentGroupId,
        long actorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OmnicommObject>> GetWantedObjectsAsync(
        IReadOnlyCollection<long> groupIds,
        IReadOnlyCollection<long> objectIds,
        long actorId,
        CancellationToken cancellationToken = default);
}