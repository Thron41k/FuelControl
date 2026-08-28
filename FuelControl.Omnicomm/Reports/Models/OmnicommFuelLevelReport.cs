namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommFuelLevelReport
{
    public string ReportId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long VehicleId { get; init; }

    public int TotalRecords { get; init; }

    public IReadOnlyList<OmnicommFuelLevelTank> Tanks { get; init; } = [];
}