using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingMatcher(
    OmnicommFuelingMatchingOptions options)
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

        ValidateOptions();

        var results = new List<FuelingMatchResult>();

        var usedEventIds = new HashSet<int>();

        foreach (var fuelingRecord in fuelingRecords)
        {
            if (fuelingRecord.Vehicle?.OmnicommObjectId
                is not { } vehicleId)
            {
                continue;
            }

            var candidates = omnicommEvents
                .Where(x =>
                    x.VehicleId == vehicleId)

                .Where(x =>
                    !usedEventIds.Contains(x.Id))

                .Select(x =>
                    CreateCandidate(
                        fuelingRecord,
                        x))

                .Where(x =>
                    x is not null)

                .Cast<FuelingMatchCandidate>()

                .OrderBy(x =>
                    x.Score)

                .ToList();

            if (candidates.Count == 0)
                continue;

            var best =
                candidates[0];

            usedEventIds.Add(
                best.Event.Id);

            results.Add(
                new FuelingMatchResult(
                    fuelingRecord,
                    best));
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

        if (timeDifference > options.TimeTolerance)
            return null;

        var volumeDifference =
            Math.Abs(
                fuelingRecord.Volume -
                omnicommEvent.VolumeLiters);

        if (volumeDifference >
            options.VolumeToleranceLiters)
        {
            return null;
        }

        var score =
            CalculateScore(
                timeDifference,
                volumeDifference);

        return new FuelingMatchCandidate(
            omnicommEvent,
            timeDifference,
            volumeDifference,
            score);
    }

    private static TimeSpan CalculateTimeDifference(
        DateTimeOffset fuelingTime,
        OmnicommFuelEvent omnicommEvent)
    {
        var time =
            fuelingTime.ToUniversalTime();

        var start =
            omnicommEvent.StartDate.ToUniversalTime();

        var end =
            omnicommEvent.EndDate.ToUniversalTime();

        if (time >= start &&
            time <= end)
        {
            return TimeSpan.Zero;
        }

        if (time < start)
        {
            return start - time;
        }

        return time - end;
    }

    private double CalculateScore(
        TimeSpan timeDifference,
        decimal volumeDifference)
    {
        var timeScore =
            options.TimeTolerance == TimeSpan.Zero
                ? 0
                : timeDifference.TotalSeconds /
                  options.TimeTolerance.TotalSeconds;

        var volumeScore =
            options.VolumeToleranceLiters == 0
                ? 0
                : (double)(
                    volumeDifference /
                    options.VolumeToleranceLiters);

        return
            timeScore * options.TimeWeight +
            volumeScore * options.VolumeWeight;
    }

    private void ValidateOptions()
    {
        if (options.VolumeToleranceLiters < 0)
        {
            throw new InvalidOperationException(
                "VolumeToleranceLiters не может быть отрицательным.");
        }

        if (options.TimeTolerance < TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "TimeTolerance не может быть отрицательным.");
        }

        if (options.TimeWeight < 0)
        {
            throw new InvalidOperationException(
                "TimeWeight не может быть отрицательным.");
        }

        if (options.VolumeWeight < 0)
        {
            throw new InvalidOperationException(
                "VolumeWeight не может быть отрицательным.");
        }

        if (options.TimeWeight == 0 &&
            options.VolumeWeight == 0)
        {
            throw new InvalidOperationException(
                "TimeWeight и VolumeWeight не могут одновременно быть равны нулю.");
        }
    }
}