using System.Net.Http;
using System.Text.Json;
using FuelControl.Omnicomm.Http;
using FuelControl.Omnicomm.Vehicles.Models;
using FuelControl.Omnicomm.Vehicles.Serialization;

namespace FuelControl.Omnicomm.Vehicles;

public sealed class OmnicommVehicleClient(
    IOmnicommApiClient apiClient)
    : IOmnicommVehicleClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        OmnicommJsonOptions.Create();

    public async Task<OmnicommVehiclesTreeResponse>
        GetVehiclesTreeAsync(
            long parentGroupId,
            long actorId,
            CancellationToken cancellationToken = default)
    {
        var requestUri =
            "/vehiclesTree" +
            $"?parentGroupID={parentGroupId}" +
            "&action=getRootGroupChildrenList" +
            $"&actorID={actorId}";

        using var response = await apiClient.SendAsync(
            () => new HttpRequestMessage(
                HttpMethod.Get,
                requestUri),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        var result =
            JsonSerializer.Deserialize<
                OmnicommVehiclesTreeResponse>(
                json,
                JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул пустой vehicles tree.");
        }

        return result;
    }

    public async Task<IReadOnlyList<OmnicommObject>> GetWantedObjectsAsync(
        IReadOnlyCollection<long> groupIds,
        IReadOnlyCollection<long> objectIds,
        long actorId,
        CancellationToken cancellationToken = default)
    {
        var wanted = new
        {
            groups = groupIds,
            objects = objectIds
        };

        var alreadyHave = new
        {
            groups = (IReadOnlyCollection<long>)Array.Empty<long>(),
            objects = (IReadOnlyCollection<long>)Array.Empty<long>()
        };

        var parameters =
            new Dictionary<string, string>
            {
                ["action"] = "getWantedList",

                ["wanted"] =
                    JsonSerializer.Serialize(wanted),

                ["alreadyHave"] =
                    JsonSerializer.Serialize(alreadyHave),

                ["allInclusive"] = "true",

                ["actorID"] =
                    actorId.ToString()
            };

        var response =
            await apiClient.PostFormAsync<OmnicommWantedListResponse>(
                "/vehiclesTree",
                parameters,
                cancellationToken);

        return response?.Objects ?? [];
    }
}