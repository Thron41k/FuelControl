using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.Extensions.Options;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingMatcher(
    IOptions<OmnicommFuelingMatchingOptions> options)
    : IOmnicommFuelingMatcher
{
    public IReadOnlyList<FuelingMatchResult> Match(
        IReadOnlyCollection<FuelingRecord> fuelingRecords,
        IReadOnlyCollection<OmnicommFuelEvent> omnicommEvents)
    {
        ArgumentNullException.ThrowIfNull(fuelingRecords);
        ArgumentNullException.ThrowIfNull(omnicommEvents);

        if (fuelingRecords.Count == 0 ||
            omnicommEvents.Count == 0)
        {
            return [];
        }

        var usedEventIds = new HashSet<int>();

        var results =
            new List<FuelingMatchResult>();

        foreach (var fuelingRecord in fuelingRecords)
        {
            if (fuelingRecord.Vehicle.OmnicommObjectId
                is not { } omnicommVehicleId)
            {
                continue;
            }

            var candidate = omnicommEvents
                .Where(x =>
                    x.VehicleId == omnicommVehicleId &&
                    x.Type == OmnicommFuelEventType.Refuel &&
                    !usedEventIds.Contains(x.Id))
                .Select(x =>
                    CreateCandidate(
                        fuelingRecord,
                        x))
                .Where(x => x is not null)
                .Cast<FuelingMatchCandidate>()
                .OrderBy(x => x.Score)
                .FirstOrDefault();

            if (candidate is null)
            {
                continue;
            }

            usedEventIds.Add(
                candidate.Event.Id);

            results.Add(
                new FuelingMatchResult(
                    fuelingRecord,
                    candidate));
        }

        return results;
    }

    private FuelingMatchCandidate? CreateCandidate(
        FuelingRecord fuelingRecord,
        OmnicommFuelEvent omnicommEvent)
    {
        var timeDifference =
            CalculateTimeDifference(
                fuelingRecord.FuelingDateTime,
                omnicommEvent);

        if (timeDifference >
            options.Value.MaxTimeDifference)
        {
            return null;
        }

        var volumeDifference =
            Math.Abs(
                fuelingRecord.Volume -
                omnicommEvent.VolumeLiters);

        var maxVolumeDifference =
            Math.Max(
                options.Value.MaxVolumeDifference,
                fuelingRecord.Volume *
                options.Value.MaxVolumeDeviationPercent);

        if (volumeDifference >
            maxVolumeDifference)
        {
            return null;
        }

        var score =
            CalculateScore(
                timeDifference,
                volumeDifference,
                maxVolumeDifference);

        return new FuelingMatchCandidate(
            omnicommEvent,
            timeDifference,
            volumeDifference,
            score);
    }

    private decimal CalculateScore(
        TimeSpan timeDifference,
        decimal volumeDifference,
        decimal maxVolumeDifference)
    {
        var timeScore =
            options.Value.MaxTimeDifference <= TimeSpan.Zero
                ? 0m
                : (decimal)(
                    timeDifference.TotalSeconds /
                    options.Value.MaxTimeDifference.TotalSeconds);

        var volumeScore =
            maxVolumeDifference <= 0m
                ? 0m
                : volumeDifference /
                  maxVolumeDifference;

        // Меньше score = лучше.
        return timeScore * 0.6m +
               volumeScore * 0.4m;
    }

    private static TimeSpan CalculateTimeDifference(
        DateTimeOffset fuelingDateTime,
        OmnicommFuelEvent fuelEvent)
    {
        var fuelingTime =
            fuelingDateTime.ToUniversalTime();

        var start =
            fuelEvent.StartDate.ToUniversalTime();

        var end =
            fuelEvent.EndDate.ToUniversalTime();

        if (fuelingTime >= start &&
            fuelingTime <= end)
        {
            return TimeSpan.Zero;
        }

        if (fuelingTime < start)
        {
            return start - fuelingTime;
        }

        return fuelingTime - end;
    }
}