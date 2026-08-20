using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingService(
    IOmnicommReportClient reportClient)
    : IOmnicommFuelingService
{
    public async Task<IReadOnlyList<OmnicommFuelEvent>> GetFuelingsAsync(
        IReadOnlyList<Vehicle> vehicles,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeZoneInfo userTimeZone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicles);
        ArgumentNullException.ThrowIfNull(userTimeZone);

        if (vehicles.Count == 0)
            return [];

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.",
                nameof(to));
        }

        var vehicleIds = vehicles
            .Where(x => x.OmnicommObjectId.HasValue)
            .Select(x => x.OmnicommObjectId!.Value)
            .Distinct()
            .ToList();

        if (vehicleIds.Count == 0)
            return [];

        var timeZone = CreateOmnicommTimeZone(userTimeZone);

        var report = await reportClient.GetFuelEventsReportAsync(
            vehicleIds,
            from,
            to,
            timeZone,
            cancellationToken);

        return report.Events
            .Where(x => x.Type == OmnicommFuelEventType.Refuel)
            .OrderBy(x => x.EventDate)
            .ToList();
    }

    private static OmnicommTimeZone CreateOmnicommTimeZone(
        TimeZoneInfo timeZone)
    {
        var winterOffset =
            (int)timeZone.BaseUtcOffset.TotalHours;

        var summerOffset = winterOffset;

        if (timeZone.SupportsDaylightSavingTime)
        {
            summerOffset++;
        }

        return new OmnicommTimeZone(
            timeZone.Id,
            winterOffset,
            summerOffset);
    }
}