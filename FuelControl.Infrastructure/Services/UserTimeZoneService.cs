using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.JSInterop;

namespace FuelControl.Infrastructure.Services;

public sealed class UserTimeZoneService(
    IJSRuntime jsRuntime)
    : IUserTimeZoneService
{
    private TimeZoneInfo? _timeZone;

    private OmnicommTimeZone? _omnicommTimeZone;


    public async Task<TimeZoneInfo> GetAsync(
        CancellationToken cancellationToken = default)
    {
        if (_timeZone is not null)
            return _timeZone;

        var timeZoneId =
            await jsRuntime.InvokeAsync<string>(
                "fuelControl.getTimeZoneId",
                cancellationToken);

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException(
                "Не удалось определить часовой пояс пользователя.");
        }

        try
        {
            _timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException(
                $"Часовой пояс '{timeZoneId}' не найден.",
                ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new InvalidOperationException(
                $"Часовой пояс '{timeZoneId}' повреждён или недействителен.",
                ex);
        }

        return _timeZone;
    }


    public async Task<OmnicommTimeZone> GetOmnicommTimeZoneAsync(
        CancellationToken cancellationToken = default)
    {
        if (_omnicommTimeZone is not null)
            return _omnicommTimeZone;

        var timeZone =
            await GetAsync(cancellationToken);

        var winterOffset =
            GetOffsetHours(timeZone, 1);

        var summerOffset =
            GetOffsetHours(timeZone, 7);

        _omnicommTimeZone =
            new OmnicommTimeZone(
                timeZone.Id,
                winterOffset,
                summerOffset);

        return _omnicommTimeZone;
    }


    public DateTimeOffset ToLocal(
        DateTimeOffset value)
    {
        if (_timeZone is null)
        {
            throw new InvalidOperationException(
                "Часовой пояс пользователя ещё не определён.");
        }

        return TimeZoneInfo.ConvertTime(
            value,
            _timeZone);
    }


    public DateTimeOffset ToUtc(
        DateTime value)
    {
        if (_timeZone is null)
        {
            throw new InvalidOperationException(
                "Часовой пояс пользователя ещё не определён.");
        }

        var unspecified =
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Unspecified);

        var utc =
            TimeZoneInfo.ConvertTimeToUtc(
                unspecified,
                _timeZone);

        return new DateTimeOffset(
            utc,
            TimeSpan.Zero);
    }

    private static int GetOffsetHours(
        TimeZoneInfo timeZone,
        int month)
    {
        var date = new DateTime(
            2026,
            month,
            15,
            12,
            0,
            0,
            DateTimeKind.Unspecified);

        return (int)timeZone
            .GetUtcOffset(date)
            .TotalHours;
    }
}