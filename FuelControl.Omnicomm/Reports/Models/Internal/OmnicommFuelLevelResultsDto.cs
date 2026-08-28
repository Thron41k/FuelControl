using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelLevelResultsDto
{
    [JsonPropertyName("data")]
    public List<OmnicommFuelLevelDataDto> Data { get; init; } = [];
}