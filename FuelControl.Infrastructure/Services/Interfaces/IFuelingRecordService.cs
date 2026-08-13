using FuelControl.Domain.Entities;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IFuelingRecordService
{
    /// <summary>
    /// Получить список заправок за указанную дату.
    /// Если fuelTruckId указан — только для выбранного топливозаправщика.
    /// </summary>
    Task<IReadOnlyList<FuelingRecord>> GetAllAsync(
        DateOnly date,
        TimeZoneInfo timeZone,
        Guid? fuelTruckId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить заправку по идентификатору.
    /// </summary>
    Task<FuelingRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать запись о заправке.
    /// </summary>
    Task<Guid> CreateAsync(
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменить запись о заправке.
    /// </summary>
    Task UpdateAsync(
        Guid id,
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись о заправке.
    /// Связанные записи FuelingUssRecord удаляются каскадно.
    /// </summary>
    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}