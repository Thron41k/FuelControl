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
        // 1. Получаем технику
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
        // 2. Формируем период
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
        // 3. Получаем актуальный отчёт Omnicomm
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
        // 4. Получаем все наши записи заправок
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
                .OrderBy(x => x.FuelingDateTime)
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
        // 5. Получаем существующие связи.
        //
        // Они нужны только для определения:
        //   - создать новую связь;
        //   - обновить существующую.
        //
        // НЕЛЬЗЯ использовать OmnicommEventId для определения
        // соответствия с текущим отчётом.
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

        var linksByFuelingRecordId =
            existingLinks
                .ToDictionary(
                    x => x.FuelingRecordId);

        // ---------------------------------------------------------
        // 6. Сопоставляем ВСЕ записи текущего периода
        // с ВСЕМИ событиями текущего отчёта.
        //
        // OmnicommEventId здесь не используется как идентификатор.
        // Matcher использует технику + время + объём.
        // ---------------------------------------------------------

        var matches =
            matcher.Match(
                fuelingRecords,
                omnicommData.Events);

        var matchedFuelingRecordIds =
            new HashSet<Guid>();

        var matchedOmnicommEventIds =
            new HashSet<int>();

        var created = 0;
        var updated = 0;

        // ---------------------------------------------------------
        // 7. Сохраняем результаты нового сопоставления
        // ---------------------------------------------------------

        foreach (var match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fuelingRecord =
                match.FuelingRecord;

            var fuelEvent =
                match.Candidate.Event;

            matchedFuelingRecordIds.Add(
                fuelingRecord.Id);

            matchedOmnicommEventIds.Add(
                fuelEvent.Id);

            if (linksByFuelingRecordId.TryGetValue(
                    fuelingRecord.Id,
                    out var existingLink))
            {
                existingLink.Update(
                    fuelEvent.Id,
                    omnicommData.ReportId,
                    fuelEvent.VehicleId,
                    fuelEvent.Name,
                    fuelEvent.StartDate,
                    fuelEvent.EndDate,
                    fuelEvent.VolumeLiters,
                    matchedBy);

                updated++;

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

            linksByFuelingRecordId[
                fuelingRecord.Id] = entity;

            created++;
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