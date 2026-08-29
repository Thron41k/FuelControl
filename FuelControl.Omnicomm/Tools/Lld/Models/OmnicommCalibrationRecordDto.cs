using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Tools.Lld.Models;

internal sealed class OmnicommCalibrationRecordDto
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("liters")]
    public decimal Liters { get; init; }
}