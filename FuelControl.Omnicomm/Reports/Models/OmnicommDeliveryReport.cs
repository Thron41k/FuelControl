// FuelControl.Omnicomm/Reports/Models/OmnicommDeliveryReport.cs
namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommDeliveryReport
{
    public string ReportId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int TotalRecords { get; init; }

    public IReadOnlyList<OmnicommDeliveryEvent> Events { get; init; } = [];
}