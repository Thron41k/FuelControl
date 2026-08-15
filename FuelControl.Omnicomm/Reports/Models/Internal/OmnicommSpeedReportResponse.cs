namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommSpeedReportResponse
{
    public string Id { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public OmnicommSpeedResults? Results { get; init; }
}