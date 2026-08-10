using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommVehicleImportService
{
    Task<OmnicommWantedListResponse> ImportAsync(
        CancellationToken cancellationToken = default);
}