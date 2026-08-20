using FuelControl.Domain.Entities;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommFuelingService
{
    Task<IReadOnlyList<OmnicommFuelEvent>> GetFuelingsAsync(
        IReadOnlyList<Vehicle> vehicles,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeZoneInfo userTimeZone,
        CancellationToken cancellationToken = default);
}