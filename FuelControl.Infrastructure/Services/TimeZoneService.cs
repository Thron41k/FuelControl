using FuelControl.Infrastructure.Services.Interfaces;

namespace FuelControl.Infrastructure.Services;

public sealed class TimeZoneService
    : ITimeZoneService
{
    public TimeZoneInfo GetCurrentTimeZone()
    {
        return TimeZoneInfo.Local;
    }

    public DateTimeOffset ToUtc(
        DateTime localDateTime)
    {
        var timeZone = GetCurrentTimeZone();

        var offset = timeZone.GetUtcOffset(
            localDateTime);

        var local = new DateTimeOffset(
            localDateTime,
            offset);

        return local.ToUniversalTime();
    }

    public DateTimeOffset ToLocal(
        DateTimeOffset utcDateTime)
    {
        var timeZone = GetCurrentTimeZone();

        return TimeZoneInfo.ConvertTime(
            utcDateTime,
            timeZone);
    }

    public string GetCurrentTimeZoneId()
    {
        return GetCurrentTimeZone().Id;
    }
}