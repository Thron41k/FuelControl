namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelLevelPointDto
{
    public long Timestamp { get; init; }

    public decimal FuelLiters { get; init; }
}