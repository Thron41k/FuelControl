namespace FuelControl.Omnicomm.Tools.Lld.Models;

public sealed class LldCalibrationTable
{
    public int SensorNumber { get; init; }

    public long TerminalId { get; init; }

    public int TankNumber { get; init; }

    public int MultiTankNumber { get; init; }

    public IReadOnlyList<LldCalibrationRecord> Records { get; init; } = [];
}