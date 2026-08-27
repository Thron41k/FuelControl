namespace FuelControl.Domain.Entities;

public sealed class FuelTruck
{
    public Guid Id { get; private set; }

    public Guid VehicleId { get; private set; }

    public Vehicle Vehicle { get; private set; } = null!;

    public ICollection<FuelTruckOmnicommBinding> OmnicommBindings
    {
        get;
        private set;
    } = new List<FuelTruckOmnicommBinding>();

    private FuelTruck()
    {
    }

    public FuelTruck(Guid vehicleId)
    {
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указана техника.",
                nameof(vehicleId));
        }

        Id = Guid.NewGuid();
        VehicleId = vehicleId;
    }

    public void ChangeVehicle(Guid vehicleId)
    {
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указана техника.",
                nameof(vehicleId));
        }

        VehicleId = vehicleId;
    }
}