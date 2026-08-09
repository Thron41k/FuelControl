namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommDeliveryResults
{
    public int Total { get; init; }

    public int Records { get; init; }

    public List<OmnicommDeliveryRowDto> Rows { get; init; } = [];
}