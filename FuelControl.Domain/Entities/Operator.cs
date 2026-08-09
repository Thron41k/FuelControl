namespace FuelControl.Domain.Entities;

public sealed class Operator
{
    public Guid Id { get; private set; }

    public string FullName { get; private set; } = null!;

    public string? PersonnelNumber { get; private set; }

    public bool IsActive { get; private set; }

    private Operator()
    {
    }

    public Operator(
        string fullName,
        string? personnelNumber = null)
    {
        Id = Guid.NewGuid();

        FullName = fullName;
        PersonnelNumber = personnelNumber;

        IsActive = true;
    }

    public void Update(
        string fullName,
        string? personnelNumber)
    {
        FullName = fullName;
        PersonnelNumber = personnelNumber;
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