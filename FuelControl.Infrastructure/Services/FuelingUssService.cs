using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelingUssService(
    FuelControlDbContext dbContext,
    IUserAuthorizationService authorization,
    IOmnicommReportClient omnicommReportClient)
    : IFuelingUssService
{
    public async Task<IReadOnlyList<OmnicommDeliveryEvent>> GetAvailableEventsAsync(
        Guid fuelTruckId,
        DateOnly date,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var fuelTruck = await dbContext.FuelTrucks
                            .AsNoTracking()
                            .Include(x => x.Vehicle)
                            .Include(x => x.UssVehicle)
                            .SingleOrDefaultAsync(
                                x => x.Id == fuelTruckId,
                                cancellationToken)
                        ?? throw new InvalidOperationException(
                            "Топливозаправщик не найден.");

        await authorization.EnsureBranchAccessAsync(
            fuelTruck.Vehicle.BranchId,
            cancellationToken);

        if (fuelTruck.Vehicle is null)
        {
            throw new InvalidOperationException(
                "За топливозаправщиком не закреплена техника.");
        }

        if (fuelTruck.UssVehicle?.OmnicommObjectId
            is not { } ussOmnicommId)
        {
            throw new InvalidOperationException(
                "Для топливозаправщика не назначена " +
                "техника Omnicomm для УСС.");
        }

        var (from, to) = GetDayRange(date,timeZone);

        var report = await omnicommReportClient.GetDeliveryReportAsync(
            [ussOmnicommId],
            from,
            to,
            timeZone,
            cancellationToken: cancellationToken);

        var events = report.Events
            .Where(x => x.VehicleId == ussOmnicommId)
            .OrderBy(x => x.StartDate)
            .ToList();

        if (events.Count == 0)
        {
            return [];
        }

        /*
         * Получаем события, которые уже были привязаны
         * к каким-либо заправкам.
         */
        var eventIds = events
            .Select(x => x.Id)
            .ToList();

        var attachedEventIds = await dbContext.FuelingUssRecords
            .AsNoTracking()
            .Where(x =>
                x.OmnicommReportId == report.ReportId &&
                eventIds.Contains(x.OmnicommEventId))
            .Select(x => x.OmnicommEventId)
            .ToListAsync(cancellationToken);

        var attachedIds =
            attachedEventIds.ToHashSet();

        return events
            .Where(x => !attachedIds.Contains(x.Id))
            .ToList();
    }

    public async Task<IReadOnlyList<FuelingUssRecord>> GetByFuelingRecordIdAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var fuelingRecord =
            await dbContext.FuelingRecords
                .AsNoTracking()
                .Include(x => x.FuelTruck)
                .ThenInclude(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == fuelingRecordId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Заправка не найдена.");

        await authorization.EnsureBranchAccessAsync(
            fuelingRecord.FuelTruck.Vehicle.BranchId,
            cancellationToken);

        return await dbContext.FuelingUssRecords
            .AsNoTracking()
            .Where(x => x.FuelingRecordId == fuelingRecordId)
            .OrderBy(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AttachAsync(
        Guid fuelingRecordId,
        IReadOnlyList<int> omnicommEventIds,
        OmnicommTimeZone? timeZone,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var userId =
            await authorization.GetCurrentUserIdAsync(
                cancellationToken);

        ArgumentNullException.ThrowIfNull(omnicommEventIds);

        var selectedIds = omnicommEventIds
            .Distinct()
            .ToHashSet();

        if (selectedIds.Count == 0)
        {
            throw new ArgumentException(
                "Не выбраны показания УСС.",
                nameof(omnicommEventIds));
        }

        var fuelingRecord =
            await dbContext.FuelingRecords
                .Include(x => x.FuelTruck)
                .ThenInclude(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == fuelingRecordId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Заправка не найдена.");

        await authorization.EnsureBranchAccessAsync(
            fuelingRecord.FuelTruck.Vehicle.BranchId,
            cancellationToken);

        var fuelTruck = await dbContext.FuelTrucks
                            .AsNoTracking()
                            .Include(x => x.Vehicle).Include(fuelTruck => fuelTruck.UssVehicle)
                            .SingleOrDefaultAsync(
                x => x.Id == fuelingRecord.FuelTruckId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Топливозаправщик не найден.");

        if (fuelTruck.Vehicle is null)
        {
            throw new InvalidOperationException(
                "За топливозаправщиком не закреплена техника.");
        }

        if (fuelTruck.UssVehicle?.OmnicommObjectId
            is not { } ussOmnicommId)
        {
            throw new InvalidOperationException(
                "Для топливозаправщика не назначена " +
                "техника Omnicomm для УСС.");
        }

        var (from, to) = GetDayRange(GetDateInTimeZone(fuelingRecord.FuelingDateTime, timeZone), timeZone);

        /*
         * Получаем актуальный отчёт непосредственно перед
         * сохранением привязки.
         */
        var report = await omnicommReportClient.GetDeliveryReportAsync(
            [ussOmnicommId],
            from,
            to,
            timeZone,
            cancellationToken: cancellationToken);

        var selectedEvents = report.Events
            .Where(x =>
                selectedIds.Contains(x.Id) &&
                x.VehicleId == ussOmnicommId)
            .ToList();

        if (selectedEvents.Count != selectedIds.Count)
        {
            var foundIds = selectedEvents
                .Select(x => x.Id)
                .ToHashSet();

            var missingIds = selectedIds
                .Where(x => !foundIds.Contains(x))
                .ToArray();

            throw new InvalidOperationException(
                "Некоторые выбранные показания УСС не найдены " +
                $"или относятся к другому топливозаправщику. " +
                $"ID: {string.Join(", ", missingIds)}.");
        }

        /*
         * Проверяем, не были ли события уже привязаны
         * к другой заправке.
         */
        var alreadyAttached = await dbContext.FuelingUssRecords
            .AsNoTracking()
            .Where(x =>
                x.OmnicommReportId == report.ReportId &&
                selectedIds.Contains(x.OmnicommEventId) &&
                x.FuelingRecordId != fuelingRecordId)
            .Select(x => new
            {
                x.OmnicommEventId,
                x.FuelingRecordId
            })
            .ToListAsync(cancellationToken);

        if (alreadyAttached.Count > 0)
        {
            var ids = alreadyAttached
                .Select(x => x.OmnicommEventId)
                .Distinct()
                .Order()
                .ToArray();

            throw new InvalidOperationException(
                "Некоторые показания УСС уже привязаны " +
                "к другой заправке. " +
                $"ID: {string.Join(", ", ids)}.");
        }

        foreach (var deliveryEvent in selectedEvents)
        {
            var ussRecord = new FuelingUssRecord(
                fuelingRecord.Id,
                deliveryEvent.Id,
                report.ReportId,
                deliveryEvent.VehicleId,
                deliveryEvent.Name,
                deliveryEvent.VolumeLiters,
                deliveryEvent.StartDate,
                deliveryEvent.EndDate,
                userId);

            dbContext.FuelingUssRecords.Add(ussRecord);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DetachAsync(
        Guid fuelingUssRecordId,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        var ussRecord =
            await dbContext.FuelingUssRecords
                .Include(x => x.FuelingRecord)
                .ThenInclude(x => x.FuelTruck)
                .ThenInclude(x => x.Vehicle)
                .SingleOrDefaultAsync(
                    x => x.Id == fuelingUssRecordId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Показание УСС не найдено.");

        await authorization.EnsureBranchAccessAsync(
            ussRecord.FuelingRecord.FuelTruck.Vehicle.BranchId,
            cancellationToken);

        dbContext.FuelingUssRecords.Remove(ussRecord);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateOnly GetDateInTimeZone(DateTimeOffset dateTime, OmnicommTimeZone timeZone)
    {
        var targetZone = TimeZoneInfo.FindSystemTimeZoneById(
            timeZone.TimeZone);
        var targetTime = TimeZoneInfo.ConvertTime(dateTime, targetZone);
        return DateOnly.FromDateTime(targetTime.Date);
    }

    private static (
        DateTimeOffset From,
        DateTimeOffset To) GetDayRange(
            DateOnly date, OmnicommTimeZone timeZone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(
            timeZone.TimeZone);

        var localStart = date.ToDateTime(
            TimeOnly.MinValue);

        var localEnd = date
            .AddDays(1)
            .ToDateTime(
                TimeOnly.MinValue);

        var utcStart =
            new DateTimeOffset(
                    localStart,
                    timeZoneInfo.GetUtcOffset(localStart));

        var utcEnd =
            new DateTimeOffset(
                    localEnd,
                    timeZoneInfo.GetUtcOffset(localEnd));

        return (utcStart, utcEnd);
    }
}