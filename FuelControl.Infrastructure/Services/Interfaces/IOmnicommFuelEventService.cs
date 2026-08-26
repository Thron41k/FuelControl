using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommFuelEventService
{
    Task<IReadOnlyList<OmnicommFuelEvent>> GetFuelEventsAsync(
        IReadOnlyList<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);
}