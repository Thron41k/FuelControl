namespace FuelControl.Domain.Entities;

public sealed class DeliveryMatch
{
    public Guid Id { get; private set; }
    public Guid FuelingRecordId { get; private set; }

    public long OmnicommVehicleId { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public int VolumeLiters { get; private set; }

    public bool IsManual { get; private set; }
    public DateTimeOffset MatchedAt { get; private set; }
    public Guid MatchedBy { get; private set; }

    public FuelingRecord FuelingRecord { get; private set; } = null!;

    private DeliveryMatch() { }

    public DeliveryMatch(
        Guid fuelingRecordId,
        long omnicommVehicleId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int volumeLiters,
        bool isManual,
        Guid matchedBy)
    {
        Id = Guid.NewGuid();
        FuelingRecordId = fuelingRecordId;
        OmnicommVehicleId = omnicommVehicleId;
        StartDate = startDate;
        EndDate = endDate;
        VolumeLiters = volumeLiters;
        IsManual = isManual;
        MatchedAt = DateTimeOffset.UtcNow;
        MatchedBy = matchedBy;
    }
}