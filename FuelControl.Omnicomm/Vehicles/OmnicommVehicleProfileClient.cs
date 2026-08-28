using System.Text.Json;
using FuelControl.Omnicomm.Http;

namespace FuelControl.Omnicomm.Vehicles;

public sealed class OmnicommVehicleProfileClient(
    IOmnicommApiClient apiClient)
    : IOmnicommVehicleProfileClient
{
    public Task<JsonDocument?> GetAsync(
        long omnicommObjectId,
        CancellationToken cancellationToken = default)
    {
        if (omnicommObjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommObjectId));
        }

        return apiClient.GetAsync<JsonDocument>(
            $"/ls/api/v1/vehicles/profile/{omnicommObjectId}",
            cancellationToken);
    }
}