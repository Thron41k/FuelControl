using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Tools.Lld.Models;

internal sealed class OmnicommCalibrationTableDto
{
    [JsonPropertyName("sensorNmb")]
    public string SensorNumber { get; init; } = string.Empty;

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; init; } = string.Empty;

    [JsonPropertyName("tankNmb")]
    public string TankNumber { get; init; } = string.Empty;

    [JsonPropertyName("multiTankNmb")]
    public string MultiTankNumber { get; init; } = string.Empty;

    [JsonPropertyName("records")]
    public List<OmnicommCalibrationRecordDto> Records { get; init; } = [];
}