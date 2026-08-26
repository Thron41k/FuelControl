using FuelControl.Domain.Entities;
using FuelControl.Infrastructure.Services.Models;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommFuelingMatcher
{
    IReadOnlyList<FuelingMatchResult> Match(
        IReadOnlyCollection<FuelingRecord> fuelingRecords,
        IReadOnlyCollection<OmnicommFuelEvent> omnicommEvents);
}