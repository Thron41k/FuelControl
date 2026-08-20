namespace FuelControl.Domain.Entities;

public sealed class FuelingOmnicommRecord
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Наша заправка, к которой привязана запись Omnicomm.
    /// </summary>
    public Guid FuelingRecordId { get; private set; }

    public FuelingRecord FuelingRecord { get; private set; } = null!;

    /// <summary>
    /// Идентификатор события/заправки в Omnicomm.
    /// </summary>
    public int OmnicommEventId { get; private set; }

    /// <summary>
    /// Идентификатор отчёта Omnicomm,
    /// из которого получена запись.
    /// </summary>
    public string OmnicommReportId { get; private set; } = null!;

    /// <summary>
    /// Omnicomm ID техники, которая была заправлена.
    /// </summary>
    public long OmnicommVehicleId { get; private set; }

    /// <summary>
    /// Название техники в Omnicomm
    /// на момент получения записи.
    /// </summary>
    public string VehicleName { get; private set; } = string.Empty;

    /// <summary>
    /// Время начала заправки в Omnicomm.
    /// Хранится в UTC.
    /// </summary>
    public DateTimeOffset StartDate { get; private set; }

    /// <summary>
    /// Время окончания заправки в Omnicomm.
    /// Хранится в UTC.
    /// </summary>
    public DateTimeOffset EndDate { get; private set; }

    /// <summary>
    /// Объём заправки по данным Omnicomm.
    /// </summary>
    public decimal VolumeLiters { get; private set; }

    /// <summary>
    /// Время, когда запись была сопоставлена
    /// с нашей заправкой.
    /// </summary>
    public DateTimeOffset MatchedAt { get; private set; }

    /// <summary>
    /// Пользователь, выполнивший сопоставление.
    /// </summary>
    public Guid MatchedBy { get; private set; }

    private FuelingOmnicommRecord()
    {
    }

    public FuelingOmnicommRecord(
        Guid fuelingRecordId,
        int omnicommEventId,
        string omnicommReportId,
        long omnicommVehicleId,
        string vehicleName,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        decimal volumeLiters,
        Guid matchedBy)
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

        if (omnicommVehicleId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommVehicleId));
        }

        if (volumeLiters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volumeLiters));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Дата окончания заправки не может быть раньше даты начала.",
                nameof(endDate));
        }

        if (matchedBy == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан пользователь.",
                nameof(matchedBy));
        }

        Id = Guid.NewGuid();

        FuelingRecordId = fuelingRecordId;

        OmnicommEventId = omnicommEventId;
        OmnicommReportId = omnicommReportId.Trim();

        OmnicommVehicleId = omnicommVehicleId;
        VehicleName = vehicleName?.Trim() ?? string.Empty;

        StartDate = startDate;
        EndDate = endDate;

        VolumeLiters = volumeLiters;

        MatchedAt = DateTimeOffset.UtcNow;
        MatchedBy = matchedBy;
    }
    public void Update(
        int omnicommEventId,
        string omnicommReportId,
        long omnicommVehicleId,
        string vehicleName,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        decimal volumeLiters,
        Guid matchedBy)
    {
        if (omnicommEventId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(omnicommEventId));

        if (string.IsNullOrWhiteSpace(omnicommReportId))
            throw new ArgumentException(
                "Не указан идентификатор отчёта Omnicomm.",
                nameof(omnicommReportId));

        if (omnicommVehicleId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(omnicommVehicleId));

        if (volumeLiters <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(volumeLiters));

        if (endDate < startDate)
            throw new ArgumentException(
                "Дата окончания заправки не может быть раньше даты начала.",
                nameof(endDate));

        if (matchedBy == Guid.Empty)
            throw new ArgumentException(
                "Не указан пользователь.",
                nameof(matchedBy));

        OmnicommEventId = omnicommEventId;
        OmnicommReportId = omnicommReportId.Trim();

        OmnicommVehicleId = omnicommVehicleId;
        VehicleName = vehicleName?.Trim() ?? string.Empty;

        StartDate = startDate;
        EndDate = endDate;

        VolumeLiters = volumeLiters;

        MatchedAt = DateTimeOffset.UtcNow;
        MatchedBy = matchedBy;
    }

}