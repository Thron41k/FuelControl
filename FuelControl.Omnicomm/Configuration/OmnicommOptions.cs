namespace FuelControl.Omnicomm.Configuration;

public sealed class OmnicommOptions
{
    public const string SectionName = "Omnicomm";

    public string BaseUrl { get; set; } =
        "https://online.omnicomm.ru";

    public string Login { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;

    public long ParentGroupId { get; set; } = 5101;
    public long ActorId { get; set; } = 1005704;
}