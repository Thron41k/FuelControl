namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommSpeedPoint
{
    public DateTimeOffset Timestamp { get; init; }

    public decimal SpeedKmh { get; init; }
}