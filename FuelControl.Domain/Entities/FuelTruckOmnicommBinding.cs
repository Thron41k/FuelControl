using FuelControl.Domain.Enums;

namespace FuelControl.Domain.Entities;

public sealed class FuelTruckOmnicommBinding
{
    public Guid Id { get; private set; }

    public Guid FuelTruckId { get; private set; }

    public FuelTruck FuelTruck { get; private set; } = null!;

    public long OmnicommObjectId { get; private set; }

    public FuelTruckOmnicommPurpose Purpose { get; private set; }

    private FuelTruckOmnicommBinding()
    {
    }

    public FuelTruckOmnicommBinding(
        Guid fuelTruckId,
        long omnicommObjectId,
        FuelTruckOmnicommPurpose purpose)
    {
        if (fuelTruckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Не указан топливозаправщик.",
                nameof(fuelTruckId));
        }

        if (omnicommObjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommObjectId));
        }

        Id = Guid.NewGuid();

        FuelTruckId = fuelTruckId;
        OmnicommObjectId = omnicommObjectId;
        Purpose = purpose;
    }

    public void Update(
        long omnicommObjectId,
        FuelTruckOmnicommPurpose purpose)
    {
        if (omnicommObjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(omnicommObjectId));
        }

        OmnicommObjectId = omnicommObjectId;
        Purpose = purpose;
    }
}