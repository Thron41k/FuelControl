using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Reports.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommSpeedService(
    FuelControlDbContext dbContext,
    IUserAuthorizationService authorization,
    IOmnicommReportClient reportClient)
    : IOmnicommSpeedService
{
    public async Task<OmnicommSpeedReport> GetSpeedAsync(
        long vehicleId,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default)
    {
        await authorization.EnsureDispatcherAsync(
            cancellationToken);

        if (vehicleId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vehicleId));
        }

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");
        }

        var vehicle =
            await dbContext.Vehicles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.OmnicommObjectId == vehicleId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Техника с указанным OmnicommObjectId не найдена.");

        await authorization.EnsureBranchAccessAsync(
            vehicle.BranchId,
            cancellationToken);

        return await reportClient.GetSpeedReportAsync(
            [vehicleId],
            from,
            to,
            timeZone,
            cancellationToken: cancellationToken);
    }
}