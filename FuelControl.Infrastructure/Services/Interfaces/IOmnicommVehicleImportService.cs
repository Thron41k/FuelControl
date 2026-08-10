using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles;

public interface IOmnicommVehicleImportService
{
    Task<OmnicommWantedListResponse> ImportAsync(
        CancellationToken cancellationToken = default);
}