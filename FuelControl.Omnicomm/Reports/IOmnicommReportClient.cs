using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Omnicomm.Reports;

public interface IOmnicommReportClient
{
    Task<OmnicommDeliveryReport> GetDeliveryReportAsync(
        IReadOnlyList<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);

    Task<OmnicommFuelEventsReport> GetFuelEventsReportAsync(
        IReadOnlyList<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);

    Task<OmnicommSpeedReport> GetSpeedReportAsync(
        IReadOnlyList<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);
}