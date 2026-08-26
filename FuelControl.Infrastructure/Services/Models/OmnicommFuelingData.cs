using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Models;

public sealed class OmnicommFuelingData
{
    public string ReportId { get; init; } = string.Empty;

    public IReadOnlyList<OmnicommFuelEvent> Events { get; init; } = [];
}