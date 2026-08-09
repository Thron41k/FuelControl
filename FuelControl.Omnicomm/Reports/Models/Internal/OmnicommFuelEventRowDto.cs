namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelEventRowDto
{
    public int Id { get; init; }

    public long VehicleID { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Type { get; init; }

    public int Volume { get; init; }

    public long Startdate { get; init; }

    public long Enddate { get; init; }

    public long Eventdate { get; init; }

    public int TankNmb { get; init; }

    public double[]? Address { get; init; }

    public string? ParseAddress { get; init; }

    public long DriverID { get; init; }

    public string Driver { get; init; } = string.Empty;

    public bool IsFTC { get; init; }

    public bool IsLLS5 { get; init; }

    public bool Exclusion { get; init; }
}