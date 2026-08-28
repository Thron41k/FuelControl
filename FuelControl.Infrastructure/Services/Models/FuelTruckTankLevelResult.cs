using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Models;

public sealed class FuelTruckTankLevelResult
{
    public Guid FuelTruckId { get; init; }

    public Guid TankVehicleId { get; init; }

    public long OmnicommObjectId { get; init; }

    public int TankNumber { get; init; }

    public decimal TankCapacityLiters { get; init; }

    public decimal OpeningLiters { get; init; }

    public decimal ClosingLiters { get; init; }

    public DateTimeOffset OpeningTimestamp { get; init; }

    public DateTimeOffset ClosingTimestamp { get; init; }

    public decimal MinLiters { get; init; }

    public decimal MaxLiters { get; init; }

    public int PointCount { get; init; }

    public int GapCount { get; init; }

    public TimeSpan MaxGap { get; init; }

    public IReadOnlyList<FuelTruckTankLevelGap> Gaps { get; init; } = [];

    public IReadOnlyList<OmnicommFuelLevelPoint> Points { get; init; } = [];
}