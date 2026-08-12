namespace FuelControl.Domain.Entities;

public sealed class FuelTruck
{
    public Guid Id { get; private set; }

    public Guid VehicleId { get; private set; }

    public Vehicle Vehicle { get; private set; } = null!;

    private FuelTruck()
    {
    }

    public FuelTruck(Guid vehicleId)
    {
        Id = Guid.NewGuid();
        VehicleId = vehicleId;
    }

    public void ChangeVehicle(Guid vehicleId)
    {
        VehicleId = vehicleId;
    }
}