using FuelControl.Infrastructure.Persistence;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FuelControl.Infrastructure.Services;

public sealed class FuelingRecordAuthorizationService(
    FuelControlDbContext dbContext,
    IUserAuthorizationService authorization)
    : IFuelingRecordAuthorizationService
{
    public async Task<bool> CanCreateAsync(
        Guid fuelTruckId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await authorization.EnsureDispatcherAsync(
                cancellationToken);

            var branchId =
                await dbContext.FuelTrucks
                    .AsNoTracking()
                    .Where(x => x.Id == fuelTruckId)
                    .Select(x => (Guid?)x.Vehicle.BranchId)
                    .SingleOrDefaultAsync(cancellationToken);

            if (branchId is null)
            {
                return false;
            }

            await authorization.EnsureBranchAccessAsync(
                branchId.Value,
                cancellationToken);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public Task<bool> CanEditAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default)
    {
        return CanAccessRecordAsync(
            fuelingRecordId,
            requireModerator: false,
            cancellationToken);
    }

    public Task<bool> CanDeleteAsync(
        Guid fuelingRecordId,
        CancellationToken cancellationToken = default)
    {
        return CanAccessRecordAsync(
            fuelingRecordId,
            requireModerator: true,
            cancellationToken);
    }

    private async Task<bool> CanAccessRecordAsync(
        Guid fuelingRecordId,
        bool requireModerator,
        CancellationToken cancellationToken)
    {
        try
        {
            if (requireModerator)
            {
                await authorization.EnsureModeratorAsync(
                    cancellationToken);
            }
            else
            {
                await authorization.EnsureDispatcherAsync(
                    cancellationToken);
            }

            var record =
                await dbContext.FuelingRecords
                    .AsNoTracking()
                    .Where(x => x.Id == fuelingRecordId)
                    .Select(x => new
                    {
                        FuelTruckBranchId =
                            x.FuelTruck.Vehicle.BranchId,

                        VehicleBranchId =
                            x.Vehicle.BranchId,

                        OperatorBranchId =
                            x.Operator.BranchId
                    })
                    .SingleOrDefaultAsync(cancellationToken);

            if (record is null)
            {
                return false;
            }

            await authorization.EnsureBranchAccessAsync(
                record.FuelTruckBranchId,
                cancellationToken);

            await authorization.EnsureBranchAccessAsync(
                record.VehicleBranchId,
                cancellationToken);

            await authorization.EnsureBranchAccessAsync(
                record.OperatorBranchId,
                cancellationToken);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}