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
    public OmnicommFuelEvent? FindMatch(
        FuelingRecord fuelingRecord,
        IReadOnlyList<OmnicommFuelEvent> events)
    {
        if (fuelingRecord.Vehicle.OmnicommObjectId is not { } omnicommVehicleId)
            return null;

        var candidates = events
            .Where(x =>
                x.VehicleId == omnicommVehicleId &&
                x.Type == OmnicommFuelEventType.Refuel)
            .Select(x => new
            {
                Event = x,
                Score = CalculateScore(
                    fuelingRecord,
                    x)
            })
            .Where(x => x.Score.HasValue)
            .OrderByDescending(x => x.Score)
            .ToList();

        return candidates
            .FirstOrDefault()
            ?.Event;
    }

    private decimal? CalculateScore(
        FuelingRecord fuelingRecord,
        OmnicommFuelEvent fuelEvent)
    {
        var timeDifference =
            CalculateTimeDifference(
                fuelingRecord.FuelingDateTime,
                fuelEvent);

        if (timeDifference >
            options.Value.MaxTimeDifference)
        {
            return null;
        }

        var volumeDifference =
            Math.Abs(
                fuelEvent.VolumeLiters -
                fuelingRecord.Volume);

        var maxVolumeDifference =
            Math.Max(
                options.Value.MaxVolumeDifference,
                fuelingRecord.Volume *
                options.Value.MaxVolumeDeviationPercent);

        if (volumeDifference > maxVolumeDifference)
            return null;

        // Чем меньше отклонения,
        // тем выше score.
        var timeScore =
            1m -
            (decimal)(
                timeDifference.TotalSeconds /
                options.Value.MaxTimeDifference.TotalSeconds);

        var volumeScore =
            1m -
            volumeDifference /
            maxVolumeDifference;

        return timeScore * 0.6m +
               volumeScore * 0.4m;
    }

    private static TimeSpan CalculateTimeDifference(
        DateTimeOffset fuelingDateTime,
        OmnicommFuelEvent fuelEvent)
    {
        if (fuelingDateTime >= fuelEvent.StartDate &&
            fuelingDateTime <= fuelEvent.EndDate)
        {
            return TimeSpan.Zero;
        }

        if (fuelingDateTime < fuelEvent.StartDate)
        {
            return fuelEvent.StartDate -
                   fuelingDateTime;
        }

        return fuelingDateTime -
               fuelEvent.EndDate;
    }
}