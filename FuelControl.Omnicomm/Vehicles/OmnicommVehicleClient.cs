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
}