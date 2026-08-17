namespace FuelControl.Omnicomm.Reports.Models;

public sealed record OmnicommTimeZone(
    string TimeZone,
    int WinterOffset,
    int SummerOffset);