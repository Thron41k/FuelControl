using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Vehicles;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommVehicleService(
    IOmnicommVehicleClient vehicleClient)
    : IOmnicommVehicleService
{
    public async Task<IReadOnlyList<OmnicommObject>>
        GetVehiclesForBranchAsync(
            long omnicommGroupId,
            long actorId,
            CancellationToken cancellationToken = default)
    {
        var result = new List<OmnicommObject>();

        var visitedGroups = new HashSet<long>();

        await LoadGroupRecursiveAsync(
            omnicommGroupId,
            actorId,
            result,
            visitedGroups,
            cancellationToken);

        return result
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }

    private async Task LoadGroupRecursiveAsync(
        long groupId,
        long actorId,
        List<OmnicommObject> result,
        HashSet<long> visitedGroups,
        CancellationToken cancellationToken)
    {
        if (!visitedGroups.Add(groupId))
        {
            return;
        }

        var tree =
            await vehicleClient.GetVehiclesTreeAsync(
                groupId,
                actorId,
                cancellationToken);

        /*
         * В ответе Omnicomm находятся группы,
         * являющиеся непосредственными детьми groupId.
         *
         * Нам нужно найти группу groupId и взять
         * её непосредственные objects и child groups.
         */

        var group =
            tree.Groups.FirstOrDefault(
                x => x.Id == groupId);

        if (group is null)
        {
            /*
             * Для некоторых запросов Omnicomm может вернуть
             * саму группу иначе либо сразу вернуть её объекты.
             *
             * В таком случае обработаем все полученные группы.
             */
            foreach (var item in tree.Groups)
            {
                AddObjects(
                    item,
                    tree,
                    result);

                foreach (var childGroupId in item.ChildGroupIds)
                {
                    await LoadGroupRecursiveAsync(
                        childGroupId,
                        actorId,
                        result,
                        visitedGroups,
                        cancellationToken);
                }
            }

            return;
        }

        AddObjects(
            group,
            tree,
            result);

        foreach (var childGroupId in group.ChildGroupIds)
        {
            await LoadGroupRecursiveAsync(
                childGroupId,
                actorId,
                result,
                visitedGroups,
                cancellationToken);
        }
    }

    private static void AddObjects(
        OmnicommGroup group,
        OmnicommVehiclesTreeResponse tree,
        List<OmnicommObject> result)
    {
        if (group.ObjectIds.Count == 0)
        {
            return;
        }

        var objectIds =
            group.ObjectIds.ToHashSet();

        result.AddRange(
            tree.Objects.Where(
                x => objectIds.Contains(x.Id)));
    }
}