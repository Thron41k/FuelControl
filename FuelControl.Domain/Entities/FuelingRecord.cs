namespace FuelControl.Domain.Entities;

public sealed class FuelingRecord
{
    public Guid Id { get; private set; }

    public Guid FuelTruckId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid OperatorId { get; private set; }

    public DateTimeOffset FuelingDateTime { get; private set; }

    /// <summary>Объём, литры (целое).</summary>
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

    private FuelingRecord() { }

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
        if (volume <= 0)
            throw new ArgumentOutOfRangeException(nameof(volume));

        if (counterEnd < counterStart)
            throw new ArgumentException(
                "Конечное показание счётчика не может быть меньше начального.");

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
        if (volume <= 0)
            throw new ArgumentOutOfRangeException(nameof(volume));

        if (counterEnd < counterStart)
            throw new ArgumentException(
                "Конечное показание счётчика не может быть меньше начального.");

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