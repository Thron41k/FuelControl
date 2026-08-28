using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelLevelDataDto
{
    [JsonPropertyName("rawValues")]
    public List<OmnicommFuelLevelPointDto> RawValues { get; init; } = [];

    [JsonPropertyName("lls5Corrections")]
    public List<object> Lls5Corrections { get; init; } = [];

    [JsonPropertyName("tankCapacity")]
    public decimal TankCapacity { get; init; }

    [JsonPropertyName("tankNumber")]
    public int TankNumber { get; init; }

    [JsonPropertyName("approxValues")]
    public List<OmnicommFuelLevelPointDto> ApproxValues { get; init; } = [];
}