using FuelControl.Domain.Entities;
using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Interfaces;

public interface IOmnicommFuelingMatcher
{
    OmnicommFuelEvent? FindMatch(
        FuelingRecord fuelingRecord,
        IReadOnlyList<OmnicommFuelEvent> events);
}