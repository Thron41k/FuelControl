using System.Text.Json;
using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommVehicleProfile
{
    [JsonPropertyName("copsProfile")]
    public JsonElement CopsProfile { get; init; }

    [JsonPropertyName("groups")]
    public JsonElement Groups { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }
}