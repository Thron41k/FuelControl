namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommFuelEventsReport
{
    public string ReportId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int TotalRecords { get; init; }

    public IReadOnlyList<OmnicommFuelEvent> Events { get; init; } = [];
}