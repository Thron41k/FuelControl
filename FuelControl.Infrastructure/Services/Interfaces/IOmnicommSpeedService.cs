using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommSpeedService
{
    Task<OmnicommSpeedReport> GetSpeedAsync(
        long vehicleId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}