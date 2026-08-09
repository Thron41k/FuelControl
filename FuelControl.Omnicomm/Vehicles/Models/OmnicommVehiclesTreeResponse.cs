using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Models;

public sealed class OmnicommVehiclesTreeResponse
{
    public IReadOnlyList<OmnicommObject> Objects { get; init; }
        = [];

    public IReadOnlyList<OmnicommGroup> Groups { get; init; }
        = [];
}