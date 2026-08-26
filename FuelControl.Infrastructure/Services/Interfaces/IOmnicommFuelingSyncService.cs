using FuelControl.Infrastructure.Services.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommFuelingSyncService
{
    Task<FuelingOmnicommSyncResult> SyncAsync(
        DateOnly from,
        DateOnly to,
        Guid? vehicleId,
        TimeZoneInfo userTimeZone,
        Guid matchedBy,
        CancellationToken cancellationToken = default);
}