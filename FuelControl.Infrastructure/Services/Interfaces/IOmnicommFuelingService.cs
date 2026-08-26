using FuelControl.Infrastructure.Services.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommFuelingService
{
    Task<OmnicommFuelingData> GetFuelingsAsync(
        IReadOnlyCollection<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeZoneInfo userTimeZone,
        CancellationToken cancellationToken = default);
}