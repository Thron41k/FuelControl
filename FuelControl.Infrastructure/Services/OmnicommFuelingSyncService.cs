using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommFuelingSyncService(
    FuelControlDbContext dbContext,
    IOmnicommFuelingService omnicommFuelingService,
    IOmnicommFuelingMatcher matcher)
    : IOmnicommFuelingSyncService
{
    public async Task SyncAsync(
        DateOnly from,
        DateOnly to,
        Guid? vehicleId,
        TimeZoneInfo userTimeZone,
        Guid matchedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userTimeZone);

        if (to < from)
        {
            throw new ArgumentException(
                "Дата окончания не может быть раньше даты начала.",
                nameof(to));
        }

        // ------------------------------------------------------------
        // 1. Получаем технику
        // ------------------------------------------------------------

        var vehiclesQuery =
            dbContext.Vehicles
                .AsNoTracking();

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
            return;

        // ------------------------------------------------------------
        // 2. Получаем Omnicomm ID техники
        // ------------------------------------------------------------

        var omnicommVehicleIds =
            vehicles
                .Where(x => x.OmnicommObjectId.HasValue)
                .Select(x => x.OmnicommObjectId!.Value)
                .Distinct()
                .ToList();

        if (omnicommVehicleIds.Count == 0)
            return;

        // ------------------------------------------------------------
        // 3. Формируем период в часовом поясе пользователя
        // ------------------------------------------------------------

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

        // ------------------------------------------------------------
        // 4. Загружаем события из Omnicomm
        // ------------------------------------------------------------

        var omnicommData =
            await omnicommFuelingService.GetFuelingsAsync(
                omnicommVehicleIds,
                fromOffset,
                toOffset,
                userTimeZone,
                cancellationToken);

        if (omnicommData.Events.Count == 0)
            return;

        // ------------------------------------------------------------
        // 5. Получаем записи заправок из нашей БД
        // ------------------------------------------------------------

        var fuelingRecords =
            await dbContext.FuelingRecords
                .Include(x => x.Vehicle)
                .Where(x =>
                    x.FuelingDateTime >= fromOffset &&
                    x.FuelingDateTime < toOffset)
                .Where(x =>
                    !vehicleId.HasValue ||
                    x.VehicleId == vehicleId.Value)
                .ToListAsync(cancellationToken);

        if (fuelingRecords.Count == 0)
            return;

        // ------------------------------------------------------------
        // 6. Получаем существующие связи
        // ------------------------------------------------------------

        var fuelingRecordIds =
            fuelingRecords
                .Select(x => x.Id)
                .ToList();

        var existingLinks =
            await dbContext.FuelingOmnicommRecords
                .Where(x =>
                    fuelingRecordIds.Contains(
                        x.FuelingRecordId))
                .ToListAsync(cancellationToken);

        // ------------------------------------------------------------
        // 7. Выполняем сопоставление
        // ------------------------------------------------------------

        var matches =
            matcher.Match(
                fuelingRecords,
                omnicommData.Events);

        if (matches.Count == 0)
            return;

        // ------------------------------------------------------------
        // 8. Сохраняем результаты
        // ------------------------------------------------------------

        foreach (var match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fuelingRecordId =
                match.FuelingRecord.Id;

            var omnicommEvent =
                match.Candidate.Event;

            var existingLink =
                existingLinks.FirstOrDefault(
                    x =>
                        x.FuelingRecordId ==
                        fuelingRecordId);

            if (existingLink is null)
            {
                var entity =
                    new FuelingOmnicommRecord(
                        fuelingRecordId,
                        omnicommEvent.Id,
                        omnicommData.ReportId,
                        omnicommEvent.VehicleId,
                        omnicommEvent.Name,
                        omnicommEvent.StartDate,
                        omnicommEvent.EndDate,
                        omnicommEvent.VolumeLiters,
                        matchedBy);

                dbContext.FuelingOmnicommRecords.Add(
                    entity);
            }
            else
            {
                existingLink.Update(
                    omnicommEvent.Id,
                    omnicommData.ReportId,
                    omnicommEvent.VehicleId,
                    omnicommEvent.Name,
                    omnicommEvent.StartDate,
                    omnicommEvent.EndDate,
                    omnicommEvent.VolumeLiters,
                    matchedBy);
            }
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}