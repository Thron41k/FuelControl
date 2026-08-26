using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Infrastructure.Services.Models;

public sealed record FuelingMatchCandidate(
    OmnicommFuelEvent Event,
    TimeSpan TimeDifference,
    decimal VolumeDifference,
    double Score);