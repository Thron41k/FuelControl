namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommGroup
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public IReadOnlyList<long> ObjectIds { get; init; }
        = [];

    public IReadOnlyList<long> ChildGroupIds { get; init; }
        = [];
}