namespace FuelControl.Domain.Entities;

public sealed class Vehicle
{
    public Guid Id { get; private set; }

    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    public string RegistrationNumber { get; private set; } = null!;

    public string? InventoryNumber { get; private set; }

    public long? OmnicommObjectId { get; private set; }

    public bool IsActive { get; private set; }

    private Vehicle()
    {
    }

    public Vehicle(
        string name,
        string registrationNumber,
        Guid branch,
        long? omnicommObjectId,
        string? inventoryNumber = null)
    {
        Id = Guid.NewGuid();
        OmnicommObjectId = omnicommObjectId;
        Name = name;
        RegistrationNumber = registrationNumber;
        InventoryNumber = inventoryNumber;
        BranchId = branch;
        IsActive = true;
    }

    public void Update(
        string name,
        string registrationNumber,
        Guid branch,
        long? omnicommObjectId,
        string? inventoryNumber)
    {
        Name = name;
        OmnicommObjectId = omnicommObjectId;
        RegistrationNumber = registrationNumber;
        InventoryNumber = inventoryNumber;
        BranchId = branch;
    }

    public void SetOmnicommObjectId(long? objectId)
    {
        OmnicommObjectId = objectId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}