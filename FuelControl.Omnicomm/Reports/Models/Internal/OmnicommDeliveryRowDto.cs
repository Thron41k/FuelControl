namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommDeliveryRowDto
{
    public int Id { get; init; }

    public long Vehicleid { get; init; }

    public string Name { get; init; } = string.Empty;

    public long Startdate { get; init; }

    public long Enddate { get; init; }

    public int Volume { get; init; }
}