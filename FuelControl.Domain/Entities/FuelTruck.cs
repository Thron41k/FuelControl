namespace FuelControl.Domain.Entities;

public sealed class FuelTruck
{
    public Guid Id { get; private set; }

    public Guid VehicleId { get; private set; }

    public Vehicle Vehicle { get; private set; } = null!;

    /// <summary>
    /// Техника Omnicomm, используемая для получения показаний УСС.
    /// </summary>
    public Guid? UssVehicleId { get; private set; }

    public Vehicle? UssVehicle { get; private set; }

    /// <summary>
    /// Техника Omnicomm, используемая для получения
    /// остатка топлива в ёмкости АТЗ.
    /// </summary>
    public Guid? TankVehicleId { get; private set; }

    public Vehicle? TankVehicle { get; private set; }

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

    public void SetUssVehicle(Guid? vehicleId)
    {
        UssVehicleId = vehicleId;
    }

    public void SetTankVehicle(Guid? vehicleId)
    {
        TankVehicleId = vehicleId;
    }
}