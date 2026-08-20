using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.JSInterop;

namespace FuelControl.Infrastructure.Services;

public sealed class BrowserTimeZoneService(
    IJSRuntime jsRuntime) : IBrowserTimeZoneService
{
    public async Task<OmnicommTimeZone> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var result =
            await jsRuntime.InvokeAsync<BrowserTimeZoneResult>(
                "fuelControl.getTimeZone",
                cancellationToken);

        if (string.IsNullOrWhiteSpace(result.TimeZone))
        {
            throw new InvalidOperationException(
                "Не удалось определить часовой пояс браузера.");
        }

        return new OmnicommTimeZone(
            result.TimeZone,
            result.WinterOffset,
            result.SummerOffset);
    }

    private sealed class BrowserTimeZoneResult
    {
        public string TimeZone { get; init; } = string.Empty;

        public int WinterOffset { get; init; }

        public int SummerOffset { get; init; }

        public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
    }
}