namespace FuelControl.Omnicomm.Reports.Models;

public sealed class OmnicommSpeedReport
{
    public string ReportId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal MaximalSpeedKmh { get; init; }

    public IReadOnlyList<OmnicommSpeedPoint> Points { get; init; } = [];

    private static readonly TimeSpan MaxSpeedPointAge =
        TimeSpan.FromMinutes(2);

    public decimal? GetMaxSpeed(
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var previousPoint = Points
            .Where(x =>
                x.Timestamp <= from &&
                from - x.Timestamp <= MaxSpeedPointAge)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();

        var intervalPoints = Points
            .Where(x =>
                x.Timestamp > from &&
                x.Timestamp <= to);

        var speeds = intervalPoints
            .Select(x => x.SpeedKmh)
            .ToList();

        if (previousPoint is not null)
        {
            speeds.Add(previousPoint.SpeedKmh);
        }

        if (speeds.Count == 0)
        {
            return null;
        }

        return speeds.Max();
    }
}