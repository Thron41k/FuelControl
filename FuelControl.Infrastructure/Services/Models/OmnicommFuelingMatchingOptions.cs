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
    /// <summary>
    /// Максимально допустимое отклонение объёма заправки.
    /// </summary>
    public decimal VolumeToleranceLiters { get; init; } = 20m;

    /// <summary>
    /// Максимальное расстояние по времени от записи заправки
    /// до начала или окончания события Omnicomm.
    /// </summary>
    public TimeSpan TimeTolerance { get; init; } =
        TimeSpan.FromMinutes(30);

    /// <summary>
    /// Вес временного соответствия при расчёте оценки.
    /// </summary>
    public double TimeWeight { get; init; } = 0.6;

    /// <summary>
    /// Вес соответствия объёма при расчёте оценки.
    /// </summary>
    public double VolumeWeight { get; init; } = 0.4;
}