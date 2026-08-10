using FuelControl.Omnicomm.Configuration;
using FuelControl.Omnicomm.Vehicles;
using FuelControl.Omnicomm.Vehicles.Models;
using FuelControl.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommBranchService(
    IOmnicommVehicleClient vehicleClient,
    IOptions<OmnicommOptions> options)
    : IOmnicommBranchService
{
    private readonly OmnicommOptions _options = options.Value;

    public async Task<IReadOnlyList<OmnicommGroup>> GetBranchesAsync(
        CancellationToken cancellationToken = default)
    {
        var response =
            await vehicleClient.GetVehiclesTreeAsync(
                _options.ParentGroupId,
                _options.ActorId,
                cancellationToken);

        return response.Groups;
    }
}