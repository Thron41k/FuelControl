namespace FuelControl.Infrastructure.Services.Interfaces;

public interface ITimeZoneService
{
    TimeZoneInfo GetCurrentTimeZone();

    DateTimeOffset ToUtc(
        DateTime localDateTime);

    DateTimeOffset ToLocal(
        DateTimeOffset utcDateTime);

    string GetCurrentTimeZoneId();
}