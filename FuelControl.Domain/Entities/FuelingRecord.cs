namespace FuelControl.Domain.Entities;

public sealed class FuelingRecord
{
    public Guid Id { get; private set; }

    public Guid FuelTruckId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid OperatorId { get; private set; }

    public DateTimeOffset FuelingDateTime { get; private set; }

    /// <summary>
    /// Объём заправки, литры.
    /// </summary>
    public int Volume { get; private set; }

    public int CounterStart { get; private set; }
    public int CounterEnd { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public FuelTruck FuelTruck { get; private set; } = null!;
    public Vehicle Vehicle { get; private set; } = null!;
    public Operator Operator { get; private set; } = null!;

    /// <summary>
    /// Показания УСС, привязанные к данной заправке.
    /// Одна заправка может иметь несколько записей УСС.
    /// </summary>
    public ICollection<FuelingUssRecord> UssRecords { get; private set; }
        = new List<FuelingUssRecord>();

    private FuelingRecord()
    {
    }

    public FuelingRecord(
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        Guid createdBy)
    {
        if (fuelTruckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан топливозаправщик.",
                nameof(fuelTruckId));
        }

        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указана техника.",
                nameof(vehicleId));
        }

        if (operatorId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан водитель.",
                nameof(operatorId));
        }

        if (volume <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volume),
                "Объём заправки должен быть больше нуля.");
        }

        if (counterStart < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(counterStart),
                "Начальное показание счётчика не может быть отрицательным.");
        }

        if (counterEnd < counterStart)
        {
            throw new ArgumentException(
                "Конечное показание счётчика не может быть меньше начального.",
                nameof(counterEnd));
        }

        Id = Guid.NewGuid();

        FuelTruckId = fuelTruckId;
        VehicleId = vehicleId;
        OperatorId = operatorId;

        FuelingDateTime = fuelingDateTime;

        Volume = volume;

        CounterStart = counterStart;
        CounterEnd = counterEnd;

        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public void Update(
        Guid fuelTruckId,
        Guid vehicleId,
        Guid operatorId,
        DateTimeOffset fuelingDateTime,
        int volume,
        int counterStart,
        int counterEnd,
        Guid updatedBy)
    {
        if (fuelTruckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан топливозаправщик.",
                nameof(fuelTruckId));
        }

        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указана техника.",
                nameof(vehicleId));
        }

        if (operatorId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан водитель.",
                nameof(operatorId));
        }

        if (volume <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volume),
                "Объём заправки должен быть больше нуля.");
        }

        if (counterStart < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(counterStart),
                "Начальное показание счётчика не может быть отрицательным.");
        }

        if (counterEnd < counterStart)
        {
            throw new ArgumentException(
                "Конечное показание счётчика не может быть меньше начального.",
                nameof(counterEnd));
        }

        FuelTruckId = fuelTruckId;
        VehicleId = vehicleId;
        OperatorId = operatorId;

        FuelingDateTime = fuelingDateTime;

        Volume = volume;

        CounterStart = counterStart;
        CounterEnd = counterEnd;

        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}