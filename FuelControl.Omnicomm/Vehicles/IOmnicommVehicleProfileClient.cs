using System.Text.Json;

namespace FuelControl.Omnicomm.Vehicles;

public interface IOmnicommVehicleProfileClient
{
    Task<JsonDocument?> GetAsync(
        long omnicommObjectId,
        CancellationToken cancellationToken = default);
}