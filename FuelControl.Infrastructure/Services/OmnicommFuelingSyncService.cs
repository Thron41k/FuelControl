using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingSyncService(
    FuelControlDbContext dbContext,
    IOmnicommFuelingService omnicommFuelingService)
    : IOmnicommFuelingSyncService
{
    // Допустимое отклонение объёма.
    private const decimal VolumeToleranceLiters = 20m;

    // Допустимое расстояние от времени заправки
    // до события Omnicomm.
    private static readonly TimeSpan TimeTolerance =
        TimeSpan.FromMinutes(30);

    public async Task<FuelingOmnicommSyncResult> SyncAsync(
        DateOnly from,
        DateOnly to,
        Guid? vehicleId,
        TimeZoneInfo userTimeZone,
        Guid matchedBy,
        CancellationToken cancellationToken = default)
    {
        if (matchedBy == Guid.Empty)
            throw new ArgumentException(
                "Не указан пользователь.",
                nameof(matchedBy));

        if (to < from)
            throw new ArgumentException(
                "Дата окончания не может быть раньше даты начала.");

        var vehiclesQuery =
            dbContext.Vehicles
                .AsNoTracking()
                .Where(x => x.OmnicommObjectId.HasValue);

        if (vehicleId.HasValue)
        {
            vehiclesQuery = vehiclesQuery
                .Where(x => x.Id == vehicleId.Value);
        }

        var vehicles = await vehiclesQuery
            .ToListAsync(cancellationToken);

        if (vehicles.Count == 0)
        {
            return new FuelingOmnicommSyncResult(
                0,
                0,
                0,
                0,
                0);
        }

        var vehicleIds = vehicles
            .Select(x => x.Id)
            .ToHashSet();

        var omnicommVehicleIds = vehicles
            .Select(x => x.OmnicommObjectId!.Value)
            .ToHashSet();

        var fromDateTime = from.ToDateTime(
            TimeOnly.MinValue,
            DateTimeKind.Unspecified);

        var toDateTime = to
            .AddDays(1)
            .ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified);

        var localFrom = new DateTimeOffset(
            fromDateTime,
            userTimeZone.GetUtcOffset(fromDateTime));

        var localTo = new DateTimeOffset(
            toDateTime,
            userTimeZone.GetUtcOffset(toDateTime));

        /*
         * Загружаем наши заправки.
         *
         * Важно: сравнение периода выполняется через UTC,
         * поэтому в БД не должно быть зависимости
         * от часового пояса сервера.
         */
        var fromUtc = localFrom.ToUniversalTime();
        var toUtc = localTo.ToUniversalTime();

        var fuelingRecords = await dbContext.FuelingRecords
            .Include(x => x.FuelTruck)
            .Include(x => x.Vehicle)
            .Where(x =>
                vehicleIds.Contains(x.VehicleId) &&
                x.FuelingDateTime >= fromUtc &&
                x.FuelingDateTime < toUtc)
            .ToListAsync(cancellationToken);

        /*
         * Получаем события Omnicomm.
         */
        var omnicommEvents =
            await omnicommFuelingService.GetFuelingsAsync(
                vehicles,
                localFrom,
                localTo,
                userTimeZone,
                cancellationToken);

        /*
         * Загружаем существующие связи.
         */
        var fuelingRecordIds = fuelingRecords
            .Select(x => x.Id)
            .ToHashSet();

        var existingLinks = await dbContext
            .FuelingOmnicommRecords
            .Where(x => fuelingRecordIds.Contains(x.FuelingRecordId))
            .ToListAsync(cancellationToken);

        /*
         * Чтобы один Omnicomm event
         * не был привязан к нескольким заправкам.
         */
        var usedOmnicommEventIds = existingLinks
            .Select(x => x.OmnicommEventId)
            .ToHashSet();

        var created = 0;
        var updated = 0;
        var unlinked = 0;

        /*
         * При полной пересинхронизации старые связи
         * должны быть освобождены.
         *
         * Поэтому сначала очищаем набор занятых событий
         * и далее строим связи заново.
         */
        usedOmnicommEventIds.Clear();

        foreach (var fuelingRecord in fuelingRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidates = omnicommEvents
                .Where(x =>
                    x.VehicleId ==
                    fuelingRecord.Vehicle.OmnicommObjectId)

                .Where(x =>
                    IsTimeMatch(
                        fuelingRecord,
                        x))

                .Where(x =>
                    IsVolumeMatch(
                        fuelingRecord,
                        x))

                .OrderBy(x =>
                    GetMatchScore(
                        fuelingRecord,
                        x))
                .ToList();

            var matchedEvent = candidates
                .FirstOrDefault(x =>
                    !usedOmnicommEventIds.Contains(x.Id));

            var existingLink = existingLinks
                .FirstOrDefault(x =>
                    x.FuelingRecordId == fuelingRecord.Id);

            if (matchedEvent is null)
            {
                if (existingLink is not null)
                {
                    dbContext.FuelingOmnicommRecords
                        .Remove(existingLink);

                    unlinked++;
                }

                continue;
            }

            usedOmnicommEventIds.Add(
                matchedEvent.Id);

            if (existingLink is null)
            {
                var entity = CreateOmnicommRecord(
                    fuelingRecord,
                    matchedEvent,
                    matchedBy);

                dbContext.FuelingOmnicommRecords
                    .Add(entity);

                created++;
            }
            else
            {
                existingLink.Update(
                    matchedEvent.Id,
                    matchedEvent.ReportId,
                    matchedEvent.VehicleId,
                    matchedEvent.Name,
                    matchedEvent.StartDate,
                    matchedEvent.EndDate,
                    matchedEvent.VolumeLiters,
                    matchedBy);

                updated++;
            }
        }

        /*
         * Удаляем старые связи с заправками,
         * которые больше не входят в выбранный набор.
         *
         * Например, если пользователь синхронизирует
         * только одну машину.
         */
        var obsoleteLinks = await dbContext
            .FuelingOmnicommRecords
            .Where(x =>
                fuelingRecordIds.Contains(x.FuelingRecordId))
            .ToListAsync(cancellationToken);

        /*
         * Здесь после основного цикла obsoleteLinks
         * фактически содержит только те связи,
         * которые могли остаться без соответствующего события.
         *
         * Основная очистка уже выполняется выше.
         */

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new FuelingOmnicommSyncResult(
            fuelingRecords.Count,
            omnicommEvents.Count,
            created,
            updated,
            unlinked);
    }

    private static FuelingOmnicommRecord CreateOmnicommRecord(
        FuelingRecord fuelingRecord,
        OmnicommFuelEvent omnicommEvent,
        Guid matchedBy)
    {
        return new FuelingOmnicommRecord(
            fuelingRecord.Id,
            omnicommEvent.Id,
            omnicommEvent.ReportId,
            omnicommEvent.VehicleId,
            omnicommEvent.Name,
            omnicommEvent.StartDate,
            omnicommEvent.EndDate,
            omnicommEvent.VolumeLiters,
            matchedBy);
    }

    private static bool IsTimeMatch(
        FuelingRecord fuelingRecord,
        OmnicommFuelEvent omnicommEvent)
    {
        var fuelingTime =
            fuelingRecord.FuelingDateTime.ToUniversalTime();

        var start =
            omnicommEvent.StartDate.ToUniversalTime();

        var end =
            omnicommEvent.EndDate.ToUniversalTime();

        /*
         * Вариант 1:
         * время нашей заправки попадает
         * непосредственно в интервал Omnicomm.
         */
        if (fuelingTime >= start &&
            fuelingTime <= end)
        {
            return true;
        }

        /*
         * Вариант 2:
         * допускаем небольшое расхождение.
         */
        var distance = fuelingTime < start
            ? start - fuelingTime
            : fuelingTime - end;

        return distance <= TimeTolerance;
    }

    private static bool IsVolumeMatch(
        FuelingRecord fuelingRecord,
        OmnicommFuelEvent omnicommEvent)
    {
        var difference =
            Math.Abs(
                fuelingRecord.Volume -
                omnicommEvent.VolumeLiters);

        return difference <= VolumeToleranceLiters;
    }

    private static double GetMatchScore(
        FuelingRecord fuelingRecord,
        OmnicommFuelEvent omnicommEvent)
    {
        var fuelingTime =
            fuelingRecord.FuelingDateTime.ToUniversalTime();

        var start =
            omnicommEvent.StartDate.ToUniversalTime();

        var end =
            omnicommEvent.EndDate.ToUniversalTime();

        double timeDistance;

        if (fuelingTime >= start &&
            fuelingTime <= end)
        {
            timeDistance = 0;
        }
        else
        {
            timeDistance =
                Math.Min(
                    Math.Abs(
                        (fuelingTime - start).TotalSeconds),
                    Math.Abs(
                        (fuelingTime - end).TotalSeconds));
        }

        var volumeDifference =
            Math.Abs(
                (double)(
                    fuelingRecord.Volume -
                    omnicommEvent.VolumeLiters));

        /*
         * Время имеет больший вес,
         * объём используется как дополнительный критерий.
         */
        return timeDistance +
               volumeDifference * 10;
    }
}