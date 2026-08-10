using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommWantedListResponse
{
    [JsonPropertyName("objects")]
    public IReadOnlyList<OmnicommObject> Objects { get; init; } = [];
    [JsonPropertyName("groups")]
    public IReadOnlyList<OmnicommGroup> Groups { get; init; } = [];
}