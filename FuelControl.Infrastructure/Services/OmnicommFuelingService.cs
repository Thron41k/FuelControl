using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingService(
    IOmnicommReportClient reportClient)
    : IOmnicommFuelingService
{
    public async Task<OmnicommFuelingData> GetFuelingsAsync(
        IReadOnlyCollection<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        TimeZoneInfo userTimeZone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicleIds);
        ArgumentNullException.ThrowIfNull(userTimeZone);

        if (vehicleIds.Count == 0)
        {
            return new OmnicommFuelingData();
        }

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.",
                nameof(to));
        }

        var ids = vehicleIds
            .Distinct()
            .ToList();

        var timeZone = CreateOmnicommTimeZone(
            userTimeZone);

        var report =
            await reportClient.GetFuelEventsReportAsync(
                ids,
                from,
                to,
                timeZone,
                cancellationToken);
        Console.WriteLine(
            $"Omnicomm report: " +
            $"ReportId={report.ReportId}, " +
            $"Total={report.TotalRecords}, " +
            $"Events={report.Events.Count}");

        foreach (var fuelEvent in report.Events)
        {
            Console.WriteLine(
                $"Event: " +
                $"Id={fuelEvent.Id}, " +
                $"VehicleId={fuelEvent.VehicleId}, " +
                $"Type={fuelEvent.Type}, " +
                $"Volume={fuelEvent.VolumeLiters}, " +
                $"Start={fuelEvent.StartDate:O}, " +
                $"End={fuelEvent.EndDate:O}");
        }
        return new OmnicommFuelingData
        {
            ReportId = report.ReportId,

            Events = report.Events
                .OrderBy(x => x.EventDate)
                .ToList()
        };
    }

    private static OmnicommTimeZone CreateOmnicommTimeZone(
        TimeZoneInfo timeZone)
    {
        var winterOffset =
            (int)timeZone.BaseUtcOffset.TotalHours;

        var summerOffset =
            winterOffset;

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