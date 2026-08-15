using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommSpeedService(
    IOmnicommReportClient reportClient)
    : IOmnicommSpeedService
{
    public Task<OmnicommSpeedReport> GetSpeedAsync(
        long vehicleId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (vehicleId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vehicleId));
        }

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");
        }

        return reportClient.GetSpeedReportAsync(
            [vehicleId],
            from,
            to,
            cancellationToken: cancellationToken);
    }
}