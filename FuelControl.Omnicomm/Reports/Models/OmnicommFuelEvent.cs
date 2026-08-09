namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommFuelEvent
{
    public int Id { get; init; }

    public long VehicleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public OmnicommFuelEventType Type { get; init; }

    /// <summary>
    /// Объём в литрах.
    /// </summary>
    public decimal VolumeLiters { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    public DateTimeOffset EventDate { get; init; }

    public int TankNumber { get; init; }

    public double? Longitude { get; init; }

    public double? Latitude { get; init; }

    public string? Address { get; init; }

    public long DriverId { get; init; }

    public string DriverName { get; init; } = string.Empty;

    public bool IsFtc { get; init; }

    public bool IsLls5 { get; init; }

    public bool Exclusion { get; init; }
}