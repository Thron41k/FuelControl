using FuelControl.Omnicomm.Configuration;
using FuelControl.Omnicomm.Vehicles;
using FuelControl.Omnicomm.Vehicles.Models;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommVehicleImportService(
    IOmnicommVehicleClient vehicleClient,
    IVehicleService vehicleService,
    IOptions<OmnicommOptions> options)
    : IOmnicommVehicleImportService
{
    private readonly OmnicommOptions _options = options.Value;

    public async Task<OmnicommWantedListResponse> ImportAsync(
        CancellationToken cancellationToken = default)
    {
        var tree =
            await vehicleClient.GetVehiclesTreeAsync(
                _options.ParentGroupId,
                _options.ActorId,
                cancellationToken);

        var groups = new Dictionary<long, OmnicommGroup>();

        var objectIds = new HashSet<long>();

        CollectTreeData(
            tree.Groups,
            groups,
            objectIds);

        foreach (var vehicleObject in tree.Objects)
        {
            objectIds.Add(vehicleObject.Id);
        }

        var existingIds = (await vehicleService.GetExistingOmnicommVehicleIdsAsync(cancellationToken)).ToHashSet();

        var objects =
            await vehicleClient.GetWantedObjectsAsync(
                groups.Keys,
                objectIds,
                _options.ActorId,
                cancellationToken);

        foreach (var omnicommObject in objects)
        {
            omnicommObject.IsAlreadyAdded = existingIds.Contains(omnicommObject.Id);
        }

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