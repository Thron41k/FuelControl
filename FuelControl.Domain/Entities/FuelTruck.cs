namespace FuelControl.Domain.Entities;

public sealed class FuelTruck
{
    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }

    public string Name { get; private set; } = null!;

    public string RegistrationNumber { get; private set; } = null!;

    public string? InventoryNumber { get; private set; }

    public long? OmnicommObjectId { get; private set; }

    public bool IsActive { get; private set; }

    private FuelTruck()
    {
    }

    public FuelTruck(
        string name,
        string registrationNumber,
        Guid branch,
        string? inventoryNumber = null)
    {
        Id = Guid.NewGuid();

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
        string? inventoryNumber)
    {
        Name = name;
        RegistrationNumber = registrationNumber;
        InventoryNumber = inventoryNumber;
        BranchId = branch;
    }

    public void SetOmnicommObjectId(long objectId)
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