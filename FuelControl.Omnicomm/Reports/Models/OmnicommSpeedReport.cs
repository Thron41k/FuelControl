namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommSpeedReport
{
    public string ReportId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal MaximalSpeedKmh { get; init; }

    public IReadOnlyList<OmnicommSpeedPoint> Points { get; init; } = [];




    /// <summary>
    /// Возвращает максимальную скорость внутри интервала события.
    ///
    /// Алгоритм:
    ///
    /// 1. Берём последнюю точку перед началом события.
    /// 2. Если она достаточно близко к началу события,
    ///    используем её как начальное состояние.
    /// 3. Берём все точки строго внутри [from; to].
    /// 4. Не расширяем интервал искусственно.
    /// 5. Точки после to никогда не участвуют.
    /// </summary>
    public decimal? GetMaxSpeed(
        DateTimeOffset from,
        DateTimeOffset to)
    {
        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");
        }

        const double startToleranceMilliseconds = 1000;

        var start = from.AddMilliseconds(
            -startToleranceMilliseconds);

        return Points
            .Where(x =>
                x.Timestamp >= start &&
                x.Timestamp <= to)
            .Select(x => x.SpeedKmh)
            .DefaultIfEmpty()
            .Max();
    }
}