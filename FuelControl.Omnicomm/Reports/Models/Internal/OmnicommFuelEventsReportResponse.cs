namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelEventsReportResponse
{
    public string Id { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public OmnicommFuelEventsResults? Results { get; init; }
}