namespace FuelControl.Domain.Entities;

public sealed class FuelingUssRecord
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Заправка, к которой привязано показание УСС.
    /// </summary>
    public Guid FuelingRecordId { get; private set; }

    public FuelingRecord FuelingRecord { get; private set; } = null!;

    /// <summary>
    /// Уникальный идентификатор события выдачи топлива в Omnicomm.
    /// </summary>
    public int OmnicommEventId { get; private set; }

    /// <summary>
    /// Идентификатор отчёта Omnicomm,
    /// из которого получено событие.
    /// </summary>
    public string OmnicommReportId { get; private set; } = null!;

    /// <summary>
    /// OmnicommId топливозаправщика,
    /// который выполнил выдачу.
    /// </summary>
    public long OmnicommFuelTruckId { get; private set; }

    /// <summary>
    /// Название события в Omnicomm.
    /// Сохраняется как историческое значение.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Объём выдачи по данным УСС, литры.
    /// </summary>
    public decimal VolumeLiters { get; private set; }

    /// <summary>
    /// Начало выдачи по данным Omnicomm.
    /// </summary>
    public DateTimeOffset StartDate { get; private set; }

    /// <summary>
    /// Окончание выдачи по данным Omnicomm.
    /// </summary>
    public DateTimeOffset EndDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    private FuelingUssRecord()
    {
    }

    public FuelingUssRecord(
        Guid fuelingRecordId,
        int omnicommEventId,
        string omnicommReportId,
        long omnicommFuelTruckId,
        string name,
        decimal volumeLiters,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid createdBy)
    {
        if (fuelingRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указана заправка.",
                nameof(fuelingRecordId));
        }

        if (omnicommEventId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommEventId));
        }

        if (string.IsNullOrWhiteSpace(omnicommReportId))
        {
            throw new ArgumentException(
                "Не указан идентификатор отчёта Omnicomm.",
                nameof(omnicommReportId));
        }

        if (omnicommFuelTruckId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommFuelTruckId));
        }

        if (volumeLiters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volumeLiters));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Дата окончания выдачи не может быть раньше даты начала.",
                nameof(endDate));
        }

        Id = Guid.NewGuid();

        FuelingRecordId = fuelingRecordId;

        OmnicommEventId = omnicommEventId;
        OmnicommReportId = omnicommReportId.Trim();

        OmnicommFuelTruckId = omnicommFuelTruckId;

        Name = name?.Trim() ?? string.Empty;

        VolumeLiters = volumeLiters;

        StartDate = startDate;
        EndDate = endDate;

        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }
}