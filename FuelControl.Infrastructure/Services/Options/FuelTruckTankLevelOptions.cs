namespace FuelControl.Infrastructure.Services.Options;

public sealed class FuelTruckTankLevelOptions
{
    /// <summary>
    /// Максимально допустимый интервал между соседними точками.
    /// </summary>
    public TimeSpan MaxAllowedGap { get; set; } =
        TimeSpan.FromMinutes(30);
}