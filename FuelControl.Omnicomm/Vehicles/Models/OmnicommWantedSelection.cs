namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommWantedSelection
{
    public IReadOnlyList<long> Groups { get; init; }
        = [];

    public IReadOnlyList<long> Objects { get; init; }
        = [];
}