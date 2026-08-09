using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Domain.Entities;

public sealed class FuelEventMatch
{
    public Guid Id { get; private set; }
    public Guid FuelingRecordId { get; private set; }

    public long OmnicommVehicleId { get; private set; }
    public FuelEventType EventType { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public int VolumeLiters { get; private set; }

    public bool IsManual { get; private set; }
    public DateTimeOffset MatchedAt { get; private set; }
    public Guid MatchedBy { get; private set; }

    public FuelingRecord FuelingRecord { get; private set; } = null!;

    private FuelEventMatch() { }

    public FuelEventMatch(
        Guid fuelingRecordId,
        long omnicommVehicleId,
        FuelEventType eventType,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int volumeLiters,
        bool isManual,
        Guid matchedBy)
    {
        Id = Guid.NewGuid();
        FuelingRecordId = fuelingRecordId;
        OmnicommVehicleId = omnicommVehicleId;
        EventType = eventType;
        StartDate = startDate;
        EndDate = endDate;
        VolumeLiters = volumeLiters;
        IsManual = isManual;
        MatchedAt = DateTimeOffset.UtcNow;
        MatchedBy = matchedBy;
    }
}