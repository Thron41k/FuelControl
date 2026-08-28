using FuelControl.Omnicomm.Tools.Lld.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommLldService
{
    Task<IReadOnlyList<LldCalibrationTable>> GetTablesAsync(
        long omnicommObjectId,
        CancellationToken cancellationToken = default);
}