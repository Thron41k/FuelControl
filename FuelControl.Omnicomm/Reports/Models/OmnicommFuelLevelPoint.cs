namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommFuelLevelPoint
{
    public DateTimeOffset Timestamp { get; init; }

    public decimal? RawLiters { get; init; }

    public decimal? ApproxLiters { get; init; }
}