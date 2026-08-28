using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using FuelControl.Infrastructure.Services.Options;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelTruckTankLevelService(
    FuelControlDbContext dbContext,
    IOmnicommReportClient omnicommReportClient,
    IOptions<FuelTruckTankLevelOptions> options)
    : IFuelTruckTankLevelService
{
    private readonly FuelTruckTankLevelOptions _options =
        options.Value;

    public async Task<FuelTruckTankLevelResult> GetAsync(
        Guid fuelTruckId,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default)
    {
        if (fuelTruckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан топливозаправщик.",
                nameof(fuelTruckId));
        }

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");
        }

        ArgumentNullException.ThrowIfNull(timeZone);

        var fuelTruck =
            await dbContext.FuelTrucks
                .AsNoTracking()
                .Include(x => x.TankVehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == fuelTruckId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        if (fuelTruck.TankVehicleId is not { } tankVehicleId)
        {
            throw new InvalidOperationException(
                "Для топливозаправщика не назначена " +
                "техника Omnicomm для емкости.");
        }

        if (fuelTruck.TankVehicle is null)
        {
            throw new InvalidOperationException(
                "Не удалось загрузить технику Omnicomm " +
                "для емкости топливозаправщика.");
        }

        if (fuelTruck.TankVehicle.OmnicommObjectId
            is not { } omnicommObjectId)
        {
            throw new InvalidOperationException(
                "У техники Omnicomm для емкости " +
                "не указан OmnicommObjectId.");
        }

        var report =
            await omnicommReportClient.GetFuelLevelReportAsync(
                [omnicommObjectId],
                from,
                to,
                timeZone,
                cancellationToken);

        if (report.Tanks.Count == 0)
        {
            throw new InvalidOperationException(
                "Omnicomm не вернул данные по емкости.");
        }

        if (report.Tanks.Count > 1)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул несколько емкостей. " +
                "Автоматический выбор емкости не выполняется.");
        }

        var tank =
            report.Tanks[0];

        var points =
            tank.Points
                .OrderBy(x => x.Timestamp)
                .ToList();

        if (points.Count == 0)
        {
            throw new InvalidOperationException(
                "Omnicomm не вернул ни одной точки уровня топлива.");
        }

        var usablePoints =
            points
                .Select(GetBestFuelValue)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

        if (usablePoints.Count == 0)
        {
            throw new InvalidOperationException(
                "В ответе Omnicomm отсутствуют значения уровня топлива.");
        }

        var gaps =
            DetectGaps(
                usablePoints,
                _options.MaxAllowedGap);

        return new FuelTruckTankLevelResult
        {
            FuelTruckId =
                fuelTruck.Id,

            TankVehicleId =
                tankVehicleId,

            OmnicommObjectId =
                omnicommObjectId,

            TankNumber =
                tank.TankNumber,

            TankCapacityLiters =
                tank.CapacityLiters,

            OpeningTimestamp =
                usablePoints[0].Timestamp,

            OpeningLiters =
                usablePoints[0].Liters,

            ClosingTimestamp =
                usablePoints[^1].Timestamp,

            ClosingLiters =
                usablePoints[^1].Liters,

            MinLiters =
                usablePoints.Min(x => x.Liters),

            MaxLiters =
                usablePoints.Max(x => x.Liters),

            PointCount =
                usablePoints.Count,

            GapCount =
                gaps.Count,

            MaxGap =
                gaps.Count == 0
                    ? TimeSpan.Zero
                    : gaps.Max(x => x.Duration),

            Gaps =
                gaps,

            Points =
                points
        };
    }

    private static TankFuelValue? GetBestFuelValue(
        OmnicommFuelLevelPoint point)
    {
        if (point.ApproxLiters.HasValue)
        {
            return new TankFuelValue(
                point.Timestamp,
                point.ApproxLiters.Value);
        }

        if (point.RawLiters.HasValue)
        {
            return new TankFuelValue(
                point.Timestamp,
                point.RawLiters.Value);
        }

        return null;
    }

    private static IReadOnlyList<FuelTruckTankLevelGap> DetectGaps(
        IReadOnlyList<TankFuelValue> points,
        TimeSpan maxAllowedGap)
    {
        if (points.Count < 2)
        {
            return [];
        }

        var gaps = new List<FuelTruckTankLevelGap>();

        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1];
            var current = points[i];

            var duration =
                current.Timestamp -
                previous.Timestamp;

            if (duration > maxAllowedGap)
            {
                gaps.Add(
                    new FuelTruckTankLevelGap
                    {
                        From =
                            previous.Timestamp,

                        To =
                            current.Timestamp,

                        Duration =
                            duration
                    });
            }
        }

        return gaps;
    }

    private readonly record struct TankFuelValue(
        DateTimeOffset Timestamp,
        decimal Liters);
}