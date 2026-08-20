namespace FuelControl.Infrastructure.Services.Models;

public sealed class OmnicommFuelingMatchingOptions
{
    /// <summary>
    /// Максимальное отклонение времени.
    /// </summary>
    public TimeSpan MaxTimeDifference { get; set; }
        = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Максимальное абсолютное отклонение объёма.
    /// </summary>
    public decimal MaxVolumeDifference { get; set; }
        = 10m;

    /// <summary>
    /// Максимальное относительное отклонение объёма.
    /// Например 0.10 = 10%.
    /// </summary>
    public decimal MaxVolumeDeviationPercent { get; set; }
        = 0.10m;
}