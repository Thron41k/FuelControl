using FuelControl.Infrastructure.Services.Models;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IFuelTruckTankLevelService
{
    Task<FuelTruckTankLevelResult> GetAsync(
        Guid fuelTruckId,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);
}