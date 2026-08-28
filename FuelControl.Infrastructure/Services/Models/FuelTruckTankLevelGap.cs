namespace FuelControl.Infrastructure.Services.Models;

public sealed class FuelTruckTankLevelGap
{
    public DateTimeOffset From { get; init; }

    public DateTimeOffset To { get; init; }

    public TimeSpan Duration { get; init; }
}