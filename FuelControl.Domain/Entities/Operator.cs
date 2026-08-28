namespace FuelControl.Domain.Entities;

public sealed class Operator
{
    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public string? PersonnelNumber { get; private set; }
    public string? RfidTagId { get; private set; }

    public bool IsActive { get; private set; }

    private Operator()
    {
    }

    public Operator(
        string fullName,
        Guid branch,
        string? personnelNumber = null,
        string? rfidTagId = null)
    {
        Id = Guid.NewGuid();

        FullName = fullName;
        PersonnelNumber = personnelNumber;
        BranchId = branch;
        RfidTagId = rfidTagId;
        IsActive = true;
    }

    public void Update(
        string fullName,
        Guid branch,
        string? personnelNumber,
        string? rfidTagId)
    {
        FullName = fullName;
        PersonnelNumber = personnelNumber;
        RfidTagId = rfidTagId;
        BranchId = branch;
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