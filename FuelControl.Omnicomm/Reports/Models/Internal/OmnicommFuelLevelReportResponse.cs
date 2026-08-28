using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelLevelReportResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("results")]
    public OmnicommFuelLevelResultsDto? Results { get; init; }
}