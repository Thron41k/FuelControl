using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommWanted
{
    [JsonPropertyName("groups")]
    public IReadOnlyList<long> Groups { get; init; } = [];
    [JsonPropertyName("objects")]
    public IReadOnlyList<long> Objects { get; init; } = [];
}