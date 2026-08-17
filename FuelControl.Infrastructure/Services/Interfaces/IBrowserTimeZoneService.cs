using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IBrowserTimeZoneService
{
    Task<OmnicommTimeZone> GetAsync(
        CancellationToken cancellationToken = default);
}