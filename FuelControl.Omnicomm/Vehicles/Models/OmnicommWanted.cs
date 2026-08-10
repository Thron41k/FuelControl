namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommWanted
{
    public IReadOnlyList<long> Groups { get; init; } = [];

    public IReadOnlyList<long> Objects { get; init; } = [];
}