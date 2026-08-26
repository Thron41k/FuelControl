using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Infrastructure.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingSyncService(
    FuelControlDbContext dbContext,
    IOmnicommFuelingService omnicommFuelingService,
    IOmnicommFuelingMatcher matcher)
    : IOmnicommFuelingSyncService
{
    public async Task<FuelingOmnicommSyncResult> SyncAsync(
        DateOnly from,
        DateOnly to,
        Guid? vehicleId,
        TimeZoneInfo userTimeZone,
        Guid matchedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userTimeZone);

        if (matchedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан пользователь.",
                nameof(matchedBy));
        }

        if (to < from)
        {
            throw new ArgumentException(
                "Дата окончания не может быть раньше даты начала.");
        }

        // ---------------------------------------------------------
        // 1. Техника
        // ---------------------------------------------------------

        var vehiclesQuery =
            dbContext.Vehicles
                .AsNoTracking()
                .Where(x =>
                    x.OmnicommObjectId.HasValue);

        if (vehicleId.HasValue)
        {
            vehiclesQuery =
                vehiclesQuery.Where(
                    x => x.Id == vehicleId.Value);
        }

        var vehicles =
            await vehiclesQuery
                .ToListAsync(cancellationToken);

        if (vehicles.Count == 0)
        {
            return EmptyResult();
        }

        var omnicommVehicleIds =
            vehicles
                .Select(x => x.OmnicommObjectId!.Value)
                .Distinct()
                .ToList();

        // ---------------------------------------------------------
        // 2. Период
        // ---------------------------------------------------------

        var localFrom =
            from.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified);

        var localTo =
            to.AddDays(1)
                .ToDateTime(
                    TimeOnly.MinValue,
                    DateTimeKind.Unspecified);

        var fromOffset =
            new DateTimeOffset(
                localFrom,
                userTimeZone.GetUtcOffset(localFrom));

        var toOffset =
            new DateTimeOffset(
                localTo,
                userTimeZone.GetUtcOffset(localTo));

        var fromUtc =
            fromOffset.ToUniversalTime();

        var toUtc =
            toOffset.ToUniversalTime();

        // ---------------------------------------------------------
        // 3. Omnicomm
        // ---------------------------------------------------------

        var omnicommData =
            await omnicommFuelingService.GetFuelingsAsync(
                omnicommVehicleIds,
                fromOffset,
                toOffset,
                userTimeZone,
                cancellationToken);

        if (omnicommData.Events.Count == 0)
        {
            return new FuelingOmnicommSyncResult(
                0,
                0,
                0,
                0,
                0);
        }

        // ---------------------------------------------------------
        // 4. Наши записи заправок
        // ---------------------------------------------------------

        var selectedVehicleIds =
            vehicles
                .Select(x => x.Id)
                .ToHashSet();

        var fuelingRecords =
            await dbContext.FuelingRecords
                .Include(x => x.Vehicle)
                .Where(x =>
                    selectedVehicleIds.Contains(
                        x.VehicleId))
                .Where(x =>
                    x.FuelingDateTime >= fromUtc &&
                    x.FuelingDateTime < toUtc)
                .ToListAsync(cancellationToken);

        if (fuelingRecords.Count == 0)
        {
            return new FuelingOmnicommSyncResult(
                0,
                omnicommData.Events.Count,
                0,
                0,
                0);
        }

        // ---------------------------------------------------------
        // 5. Существующие связи
        // ---------------------------------------------------------

        var fuelingRecordIds =
            fuelingRecords
                .Select(x => x.Id)
                .ToHashSet();

        var existingLinks =
            await dbContext.FuelingOmnicommRecords
                .Where(x =>
                    fuelingRecordIds.Contains(
                        x.FuelingRecordId))
                .ToListAsync(cancellationToken);

        var linksByFuelingRecord =
            existingLinks
                .ToDictionary(
                    x => x.FuelingRecordId);

        // ---------------------------------------------------------
        // 6. Сохраняем уже существующие связи
        //
        // Они считаются подтверждёнными и не должны
        // быть переопределены другим событием.
        // ---------------------------------------------------------

        var linkedFuelingRecordIds =
            existingLinks
                .Select(x => x.FuelingRecordId)
                .ToHashSet();

        var linkedOmnicommEventIds =
            existingLinks
                .Select(x => x.OmnicommEventId)
                .ToHashSet();

        // ---------------------------------------------------------
        // 7. Свободные записи для автоматического сопоставления
        // ---------------------------------------------------------

        var unmatchedFuelingRecords =
            fuelingRecords
                .Where(x =>
                    !linkedFuelingRecordIds.Contains(x.Id))
                .ToList();

        var unmatchedOmnicommEvents =
            omnicommData.Events
                .Where(x =>
                    !linkedOmnicommEventIds.Contains(x.Id))
                .ToList();

        var matches =
            matcher.Match(
                unmatchedFuelingRecords,
                unmatchedOmnicommEvents);

        // ---------------------------------------------------------
        // 8. Сохраняем новые сопоставления
        // ---------------------------------------------------------

        var created = 0;
        var updated = 0;

        foreach (var match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fuelingRecord =
                match.FuelingRecord;

            var fuelEvent =
                match.Candidate.Event;

            if (linksByFuelingRecord.ContainsKey(
                    fuelingRecord.Id))
            {
                continue;
            }

            var entity =
                new FuelingOmnicommRecord(
                    fuelingRecord.Id,
                    fuelEvent.Id,
                    omnicommData.ReportId,
                    fuelEvent.VehicleId,
                    fuelEvent.Name,
                    fuelEvent.StartDate,
                    fuelEvent.EndDate,
                    fuelEvent.VolumeLiters,
                    matchedBy);

            dbContext.FuelingOmnicommRecords.Add(
                entity);

            linksByFuelingRecord[
                fuelingRecord.Id] = entity;

            created++;
        }

        // ---------------------------------------------------------
        // 9. Проверяем существующие связи на актуальность.
        //
        // Если Omnicomm event всё ещё есть в полученном отчёте,
        // обновляем его данные, но не меняем сам факт связи.
        // ---------------------------------------------------------

        var currentEvents =
            omnicommData.Events
                .ToDictionary(x => x.Id);

        foreach (var link in existingLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!currentEvents.TryGetValue(
                    link.OmnicommEventId,
                    out var fuelEvent))
            {
                continue;
            }

            link.Update(
                fuelEvent.Id,
                omnicommData.ReportId,
                fuelEvent.VehicleId,
                fuelEvent.Name,
                fuelEvent.StartDate,
                fuelEvent.EndDate,
                fuelEvent.VolumeLiters,
                matchedBy);

            updated++;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new FuelingOmnicommSyncResult(
            fuelingRecords.Count,
            omnicommData.Events.Count,
            created,
            updated,
            0);
    }

    private static FuelingOmnicommSyncResult EmptyResult()
    {
        return new FuelingOmnicommSyncResult(
            0,
            0,
            0,
            0,
            0);
    }
}