using FuelControl.Omnicomm.Vehicles;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommVehicleImportService(
    IOmnicommVehicleClient vehicleClient)
    : IOmnicommVehicleImportService
{
    public async Task<OmnicommWantedListResponse> ImportAsync(
        CancellationToken cancellationToken = default)
    {
        var tree =
            await vehicleClient.GetVehiclesTreeAsync(
                cancellationToken);

        var groups = new Dictionary<long, OmnicommGroup>();
        var objectIds = new HashSet<long>();

        CollectTreeData(
            tree.Groups,
            groups,
            objectIds);

        var objects =
            await vehicleClient.GetWantedObjectsAsync(
                groups.Keys,
                objectIds,
                cancellationToken);

        return new OmnicommWantedListResponse
        {
            Groups = groups.Values.ToArray(),
            Objects = objects
        };
    }

    private static void CollectTreeData(
        IEnumerable<OmnicommGroup> sourceGroups,
        IDictionary<long, OmnicommGroup> groups,
        ISet<long> objectIds)
    {
        foreach (var group in sourceGroups)
        {
            if (!groups.TryAdd(group.Id, group))
            {
                continue;
            }

            foreach (var objectId in group.ObjectIds)
            {
                objectIds.Add(objectId);
            }
        }
    }
}