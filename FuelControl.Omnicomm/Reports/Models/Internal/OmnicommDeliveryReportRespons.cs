namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommDeliveryReportResponse
{
    public string Id { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public OmnicommDeliveryResults? Results { get; init; }
}