namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelEventsResults
{
    public int Total { get; init; }

    public int Records { get; init; }

    public List<OmnicommFuelEventRowDto> Rows { get; init; } = [];
}