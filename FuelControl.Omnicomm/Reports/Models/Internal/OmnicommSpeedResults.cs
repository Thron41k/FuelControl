using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommSpeedResults
{
    [JsonPropertyName("speedData")]
    public List<OmnicommSpeedPointDto> SpeedData { get; init; } = [];

    [JsonPropertyName("maximalSpeed")]
    public decimal MaximalSpeed { get; init; }
}