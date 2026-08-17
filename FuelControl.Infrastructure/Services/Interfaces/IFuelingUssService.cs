using FuelControl.Domain.Entities;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IFuelingUssService
{
    /// <summary>
    /// Получить все доступные показания УСС
    /// выбранного топливозаправщика за указанную дату.
    ///
    /// Никакого автоматического сопоставления с техникой,
    /// объёмом или временем заправки не выполняется.
    /// </summary>
    Task<IReadOnlyList<OmnicommDeliveryEvent>> GetAvailableEventsAsync(
        Guid fuelTruckId,
        DateOnly date,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить уже привязанные показания УСС
    /// для конкретной заправки.
    /// </summary>
    Task<IReadOnlyList<FuelingUssRecord>> GetByFuelingRecordIdAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Привязать выбранные события УСС к заправке.
    ///
    /// Каждое событие Omnicomm может быть привязано
    /// только к одной заправке.
    /// </summary>
    Task AttachAsync(
        Guid fuelingRecordId,
        IReadOnlyList<int> omnicommEventIds,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отвязать показание УСС от заправки.
    /// </summary>
    Task DetachAsync(
        Guid fuelingUssRecordId,
        CancellationToken cancellationToken = default);
}