namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommObject
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string SystemType { get; init; } = string.Empty;

    public int Value { get; init; }

    public bool Flag { get; init; }

    public bool IsAlreadyAdded { get; set; }
}