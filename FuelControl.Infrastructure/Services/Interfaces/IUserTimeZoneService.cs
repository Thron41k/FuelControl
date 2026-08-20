using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IUserTimeZoneService
{
    Task<TimeZoneInfo> GetAsync(
        CancellationToken cancellationToken = default);

    Task<OmnicommTimeZone> GetOmnicommTimeZoneAsync(
        CancellationToken cancellationToken = default);

    DateTimeOffset ToLocal(
        DateTimeOffset value);

    DateTimeOffset ToUtc(
        DateTime value);
}