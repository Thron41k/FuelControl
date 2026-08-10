namespace FuelControl.Omnicomm.Vehicles.Models;

public sealed class OmnicommWantedListRequest
{
    public string Action { get; init; } = "getWantedList";

    public OmnicommWanted Wanted { get; init; } = new();

    public OmnicommWanted AlreadyHave { get; init; } = new();

    public bool AllInclusive { get; init; } = true;

    public long ActorId { get; init; }
}