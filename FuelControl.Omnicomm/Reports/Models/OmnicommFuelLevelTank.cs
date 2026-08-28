namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommFuelLevelTank
{
    public int TankNumber { get; init; }

    public decimal CapacityLiters { get; init; }

    public IReadOnlyList<OmnicommFuelLevelPoint> Points { get; init; } = [];
}