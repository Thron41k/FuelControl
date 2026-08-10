namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommWantedListResponse
{
    public IReadOnlyList<OmnicommObject> Objects { get; init; } = [];

    public IReadOnlyList<OmnicommGroup> Groups { get; init; } = [];
}