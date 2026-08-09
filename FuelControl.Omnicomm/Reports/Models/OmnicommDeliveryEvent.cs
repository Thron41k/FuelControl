// FuelControl.Omnicomm/Reports/Models/OmnicommDeliveryEvent.cs
namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommDeliveryEvent
{
    public int Id { get; init; }

    public long VehicleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    /// <summary>
    /// Объём в литрах.
    /// </summary>
    public decimal VolumeLiters { get; init; }
}