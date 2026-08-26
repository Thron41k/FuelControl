using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelEventService(
    IOmnicommReportClient reportClient)
    : IOmnicommFuelEventService
{
    public async Task<IReadOnlyList<OmnicommFuelEvent>> GetFuelEventsAsync(
        IReadOnlyList<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicleIds);

        if (vehicleIds.Count == 0)
            return [];

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания периода должна быть больше даты начала.");
        }

        var report = await reportClient.GetFuelEventsReportAsync(
            vehicleIds,
            from,
            to,
            timeZone,
            cancellationToken);

        return report.Events
            .OrderBy(x => x.StartDate)
            .ToList();
    }
}